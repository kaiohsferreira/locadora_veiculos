"""Compara o SQL da especificacao (apendices A e B) com o SQL gerado pelo EF.

Uso (modo literal, sem o 4o argumento):
    python -I compare_schema.py <apendice_A.sql> <apendice_B.sql> <gerado.sql>
    Compara literalmente contra os apendices. Codigo de saida 0.

Uso (modo com o arquivo de decisoes, 4o argumento):
    python -I compare_schema.py <apendice_A.sql> <apendice_B.sql> <gerado.sql> <desvios_aprovados.json>
    Aplica os desvios antigos (colunas de auditoria, colunas de acesso, unique trocado por
    indice comum) e liga cada diferenca restante a um item 'cobre' de um bloco do JSON.
    Codigo de saida 1 quando ha diferenca nao registrada, registro sem diferenca ou bloco sem
    registro; 0 quando todos sao zero.

Formato de cada item de 'cobre' (um texto por item; o nome vem da saida real do script):
    tabela:<tabela>                    tabela nova (cobre a tabela; as colunas dela NAO sao comparadas)
    coluna:<tabela>.<coluna>           coluna nova em tabela existente
    coluna_alterada:<tabela>.<coluna>  coluna com tipo, nulidade ou DEFAULT diferente
    check:<nome> / check_alterado:<nome>
    fk:<nome> / fk_alterado:<nome>
    unique:<nome> / unique_alterado:<nome>
    indice:<nome> / indice_alterado:<nome>
Bloco que nao muda esquema tem 'cobre': []. Linha 'TABELA DIFERENTE' nao conta: o que conta
sao as colunas que diferem. Itens 'falta:' (algo que sumiu do gerado) nunca sao registrados.
Limites: o casamento e so pelo nome; o conteudo (tipo, expressao, destino) NAO e comparado
com o que o bloco descreve. Item repetido em 'cobre' conta como erro (REGISTRO SEM DIFERENCA).
Bloco do JSON que nao e objeto (fora os campos de texto do topo) conta como BLOCO SEM REGISTRO.
"""
import json
import re
import sys


def norm_type(t):
    t = t.strip().lower()
    t = re.sub(r"\s+", " ", t)
    m = re.match(r"varchar\((\d+)\)", t)
    if m:
        return f"character varying({m.group(1)})"
    m = re.match(r"char\((\d+)\)", t)
    if m:
        return f"character({m.group(1)})"
    m = re.match(r"numeric\((\d+),\s*(\d+)\)", t)
    if m:
        return f"numeric({m.group(1)},{m.group(2)})"
    return {"timestamp": "timestamp without time zone"}.get(t, t)


def norm_expr(e):
    e = e.strip().rstrip(",").strip()
    # remove parenteses externos redundantes
    while e.startswith("(") and e.endswith(")"):
        depth = 0
        balanced = True
        for i, c in enumerate(e):
            depth += c == "("
            depth -= c == ")"
            if depth == 0 and i < len(e) - 1:
                balanced = False
                break
        if not balanced:
            break
        e = e[1:-1].strip()
    e = re.sub(r"\s+", " ", e).lower()
    e = re.sub(r"\(\s+", "(", e)
    e = re.sub(r"\s+\)", ")", e)
    return e


def norm_default(d):
    if d is None:
        return None
    d = norm_expr(d)
    return d


def split_top_level(body):
    parts, depth, cur = [], 0, ""
    for c in body:
        if c == "(":
            depth += 1
        elif c == ")":
            depth -= 1
        if c == "," and depth == 0:
            parts.append(cur)
            cur = ""
        else:
            cur += c
    if cur.strip():
        parts.append(cur)
    return [p.strip() for p in parts if p.strip()]


def strip_comments(sql):
    return "\n".join(line.split("--", 1)[0] for line in sql.splitlines())


def parse(sql):
    sql = strip_comments(sql).replace("﻿", "")
    tables, checks, fks, uniques, indexes = {}, {}, {}, {}, {}
    for m in re.finditer(r"CREATE TABLE\s+(?:IF NOT EXISTS\s+)?\"?(\w+)\"?\s*\((.*?)\);", sql, re.S | re.I):
        tname, body = m.group(1), m.group(2)
        if tname == "__EFMigrationsHistory":
            continue
        cols = {}
        for item in split_top_level(body):
            up = item.upper()
            if up.startswith("CONSTRAINT"):
                cm = re.match(r"CONSTRAINT\s+(\w+)\s+(.*)", item, re.S | re.I)
                cname, rest = cm.group(1), cm.group(2).strip()
                if rest.upper().startswith("CHECK"):
                    checks[cname] = (tname, norm_expr(rest[5:]))
                elif rest.upper().startswith("FOREIGN KEY"):
                    fm = re.match(r"FOREIGN KEY\s*\((\w+)\)\s*REFERENCES\s+(\w+)\s*\((\w+)\)(.*)", rest, re.S | re.I)
                    ondel = "CASCADE" if "CASCADE" in fm.group(4).upper() else "NO ACTION"
                    fks[cname] = (tname, fm.group(1), fm.group(2), fm.group(3), ondel)
                elif rest.upper().startswith("UNIQUE"):
                    ucols = tuple(c.strip() for c in re.search(r"\((.*?)\)", rest).group(1).split(","))
                    uniques[cname] = (tname, ucols)
                elif rest.upper().startswith("PRIMARY KEY"):
                    pass
                continue
            cm = re.match(r"(\w+)\s+(.*)", item, re.S)
            cname, rest = cm.group(1), cm.group(2)
            tm = re.match(r"(character varying\(\d+\)|character\(\d+\)|timestamp without time zone|numeric\(\d+,\s*\d+\)|varchar\(\d+\)|char\(\d+\)|\w+)", rest, re.I)
            ctype = norm_type(tm.group(1))
            after = rest[tm.end():]
            notnull = bool(re.search(r"NOT NULL|PRIMARY KEY", after, re.I))
            dm = re.search(r"DEFAULT\s+(\(.*?\)|'[^']*'|[\w.]+)", after, re.I)
            default = norm_default(dm.group(1)) if dm else None
            if re.search(r"\bUNIQUE\b", after, re.I):
                uniques[f"{tname}_{cname}_key"] = (tname, (cname,))
            cols[cname] = (ctype, notnull, default)
        tables[tname] = cols
    for m in re.finditer(r"CREATE\s+(UNIQUE\s+)?INDEX\s+(\w+)\s+ON\s+(\w+)\s*\((.*?)\);", sql, re.S | re.I):
        icols = tuple(c.strip() for c in m.group(4).split(","))
        if m.group(1):
            uniques[m.group(2)] = (m.group(3), icols)
        else:
            indexes[m.group(2)] = (m.group(3), icols)
    return tables, checks, fks, uniques, indexes


def compare(label, spec, gen, out, tipo, tipo_alterado, pai=None):
    """Acrescenta a 'out' tuplas (chave, linha, nome). Chave None = linha que nao conta (tabela diferente)."""
    for k in sorted(set(spec) | set(gen)):
        nome = k if pai is None else f"{pai}.{k}"
        if k not in gen:
            out.append((f"falta:{tipo}:{nome}", f"FALTA no gerado  | {label} | {k}: {spec[k]}", nome))
        elif k not in spec:
            out.append((f"{tipo}:{nome}", f"SOBRA no gerado  | {label} | {k}: {gen[k]}", nome))
        elif spec[k] != gen[k]:
            chave = f"{tipo_alterado}:{nome}" if tipo_alterado else None
            out.append((chave, f"DIFERENTE        | {label} | {k}: especificacao={spec[k]} gerado={gen[k]}", nome))


def comparar_tudo(s_tab, g_tab, s_chk, g_chk, s_fk, g_fk, s_uq, g_uq, s_idx, g_idx):
    out = []
    compare("tabela", {t: sorted(c) for t, c in s_tab.items()}, {t: sorted(c) for t, c in g_tab.items()}, out, "tabela", None)
    for t in sorted(set(s_tab) & set(g_tab)):
        compare(f"coluna {t}", s_tab[t], g_tab[t], out, "coluna", "coluna_alterada", pai=t)
    compare("check", s_chk, g_chk, out, "check", "check_alterado")
    compare("fk", s_fk, g_fk, out, "fk", "fk_alterado")
    compare("unique", s_uq, g_uq, out, "unique", "unique_alterado")
    compare("indice", s_idx, g_idx, out, "indice", "indice_alterado")
    return out


def parse_definicao_coluna(definicao):
    """Converte 'VARCHAR(255) NULL' (ou 'NOT NULL') no formato (tipo, not_null, default) do parse."""
    m = re.match(r"^(.+?)\s+(NOT\s+NULL|NULL)\s*$", definicao.strip(), re.I)
    if not m:
        raise ValueError(f"definicao de coluna de acesso invalida: {definicao!r}")
    return (norm_type(m.group(1)), m.group(2).upper().startswith("NOT"), None)


def apply_deviations(path, tables, uniques, indexes):
    """Aplica a especificacao os desvios aprovados e devolve a lista do que foi aplicado."""
    dev = json.load(open(path, encoding="utf-8"))
    applied = []
    audit = dev.get("colunas_auditoria")
    if audit:
        targets = sorted(tables) if audit["tabelas"] == "todas" else audit["tabelas"]
        for t in targets:
            for cname, c in audit["colunas"].items():
                if cname not in tables[t]:
                    tables[t][cname] = (c["tipo"], c["not_null"], c["default"])
                    applied.append(f"coluna {t}.{cname}")
    acesso = dev.get("colunas_acesso")
    if acesso:
        for t, colunas in acesso["colunas"].items():
            if t not in tables:
                applied.append(f"AVISO: tabela {t} nao existe na especificacao (colunas_acesso)")
                continue
            for cname, definicao in colunas.items():
                if cname not in tables[t]:
                    tables[t][cname] = parse_definicao_coluna(definicao)
                    applied.append(f"coluna {t}.{cname} (colunas_acesso)")
    for uname, iname in dev.get("unique_para_indice", {}).items():
        if uname in uniques:
            indexes[iname] = uniques.pop(uname)
            applied.append(f"unique {uname} -> indice {iname}")
        else:
            applied.append(f"AVISO: unique {uname} nao existe na especificacao")
    return applied


ANTIGOS = ("colunas_auditoria", "colunas_acesso", "unique_para_indice")


def classificar(out, caminho):
    """Liga cada diferenca a um item 'cobre' de um bloco do JSON. Imprime e devolve o codigo de saida."""
    dev = json.load(open(caminho, encoding="utf-8"))
    blocos = {}
    cobertura = {}
    sem_registro = []
    sem_diferenca = []
    for nome, bloco in dev.items():
        if nome in ANTIGOS or isinstance(bloco, str):
            continue
        if not isinstance(bloco, dict):
            sem_registro.append(nome)
            continue
        blocos[nome] = bloco
    for nome, bloco in blocos.items():
        cobre = bloco.get("cobre")
        if not isinstance(cobre, list):
            sem_registro.append(nome)
            continue
        for item in cobre:
            if not isinstance(item, str):
                sem_diferenca.append((nome, repr(item)))
            elif item in cobertura:
                sem_diferenca.append((nome, f"{item} (repetido)"))
                cobertura[item].append(nome)
            else:
                cobertura.setdefault(item, []).append(nome)
    diferencas = [(chave, linha, nome) for chave, linha, nome in out if chave is not None]
    cobriveis = {chave for chave, linha, nome in diferencas if not chave.startswith("falta:")}
    for item, nomes in cobertura.items():
        if item not in cobriveis:
            for nome in nomes:
                sem_diferenca.append((nome, item))

    nao_registradas = 0
    registradas = 0
    for chave, linha, nome in diferencas:
        if chave in cobriveis and chave in cobertura:
            registradas += 1
            print(f"REGISTRADA | {', '.join(cobertura[chave])} | {linha}")
        else:
            nao_registradas += 1
            print(f"NAO REGISTRADA | {linha}")

    for linha, tabela in [(linha, nome) for chave, linha, nome in out if chave is None]:
        prefixos = (f"coluna:{tabela}.", f"coluna_alterada:{tabela}.", f"falta:coluna:{tabela}.")
        colunas = [chave for chave, l2, n2 in diferencas if chave.startswith(prefixos)]
        registrada = len(colunas) > 0 and all(chave in cobriveis and chave in cobertura for chave in colunas)
        status = "REGISTRADA" if registrada else "NAO REGISTRADA"
        print(f"TABELA DIFERENTE (nao conta) | {status} | {linha}")

    for nome in sem_registro:
        print(f"BLOCO SEM REGISTRO | {nome}")
    for nome, item in sem_diferenca:
        print(f"REGISTRO SEM DIFERENCA | {nome} | {item}")

    print(f"Diferencas registradas: {registradas}")
    print(f"Diferencas nao registradas: {nao_registradas}")
    print(f"Registros sem diferenca: {len(sem_diferenca)}")
    print(f"Blocos sem registro: {len(sem_registro)}")
    return 1 if (nao_registradas > 0 or len(sem_diferenca) > 0 or len(sem_registro) > 0) else 0


def main():
    spec_sql = open(sys.argv[1], encoding="utf-8").read() + "\n" + open(sys.argv[2], encoding="utf-8").read()
    gen_sql = open(sys.argv[3], encoding="utf-8-sig").read()
    s_tab, s_chk, s_fk, s_uq, s_idx = parse(spec_sql)
    g_tab, g_chk, g_fk, g_uq, g_idx = parse(gen_sql)

    com_decisoes = len(sys.argv) > 4
    if com_decisoes:
        applied = apply_deviations(sys.argv[4], s_tab, s_uq, s_idx)
        print(f"Desvios aprovados aplicados a especificacao: {len(applied)}")
        for line in applied:
            print(f"  {line}")

    out = comparar_tudo(s_tab, g_tab, s_chk, g_chk, s_fk, g_fk, s_uq, g_uq, s_idx, g_idx)

    print(f"Tabelas:  especificacao={len(s_tab)} gerado={len(g_tab)}")
    print(f"Colunas:  especificacao={sum(len(c) for c in s_tab.values())} gerado={sum(len(c) for c in g_tab.values())}")
    print(f"CHECK:    especificacao={len(s_chk)} gerado={len(g_chk)}")
    print(f"FK:       especificacao={len(s_fk)} gerado={len(g_fk)}")
    print(f"UNIQUE:   especificacao={len(s_uq)} gerado={len(g_uq)}")
    print(f"Indices:  especificacao={len(s_idx)} gerado={len(g_idx)}")

    if not com_decisoes:
        print(f"Diferencas: {len(out)}")
        for chave, line, nome in out:
            print(line)
        return 0
    return classificar(out, sys.argv[4])


if __name__ == "__main__":
    sys.exit(main())
