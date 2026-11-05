"""Compara o SQL da especificacao (apendices A e B) com o SQL gerado pelo EF.

Uso: python -I compare_schema.py <apendice_A.sql> <apendice_B.sql> <gerado.sql> [desvios_aprovados.json]

Com o 4o argumento, os desvios aprovados (colunas de auditoria, colunas de acesso e
unique trocado por indice comum) sao aplicados a especificacao antes da comparacao, e o
relatorio lista cada desvio aplicado. Sem ele, a comparacao e literal contra os apendices.
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


def compare(label, spec, gen, out):
    for k in sorted(set(spec) | set(gen)):
        if k not in gen:
            out.append(f"FALTA no gerado  | {label} | {k}: {spec[k]}")
        elif k not in spec:
            out.append(f"SOBRA no gerado  | {label} | {k}: {gen[k]}")
        elif spec[k] != gen[k]:
            out.append(f"DIFERENTE        | {label} | {k}: especificacao={spec[k]} gerado={gen[k]}")


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


def main():
    spec_sql = open(sys.argv[1], encoding="utf-8").read() + "\n" + open(sys.argv[2], encoding="utf-8").read()
    gen_sql = open(sys.argv[3], encoding="utf-8-sig").read()
    s_tab, s_chk, s_fk, s_uq, s_idx = parse(spec_sql)
    g_tab, g_chk, g_fk, g_uq, g_idx = parse(gen_sql)

    if len(sys.argv) > 4:
        applied = apply_deviations(sys.argv[4], s_tab, s_uq, s_idx)
        print(f"Desvios aprovados aplicados a especificacao: {len(applied)}")
        for line in applied:
            print(f"  {line}")

    out = []
    compare("tabela", {t: sorted(c) for t, c in s_tab.items()}, {t: sorted(c) for t, c in g_tab.items()}, out)
    for t in sorted(set(s_tab) & set(g_tab)):
        compare(f"coluna {t}", s_tab[t], g_tab[t], out)
    compare("check", s_chk, g_chk, out)
    compare("fk", s_fk, g_fk, out)
    compare("unique", s_uq, g_uq, out)
    compare("indice", s_idx, g_idx, out)

    print(f"Tabelas:  especificacao={len(s_tab)} gerado={len(g_tab)}")
    print(f"Colunas:  especificacao={sum(len(c) for c in s_tab.values())} gerado={sum(len(c) for c in g_tab.values())}")
    print(f"CHECK:    especificacao={len(s_chk)} gerado={len(g_chk)}")
    print(f"FK:       especificacao={len(s_fk)} gerado={len(g_fk)}")
    print(f"UNIQUE:   especificacao={len(s_uq)} gerado={len(g_uq)}")
    print(f"Indices:  especificacao={len(s_idx)} gerado={len(g_idx)}")
    print(f"Diferencas: {len(out)}")
    for line in out:
        print(line)


if __name__ == "__main__":
    main()
