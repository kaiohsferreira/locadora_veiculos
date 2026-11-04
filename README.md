# locadora_veiculos — AutoVia

Essa é uma aplicação para o trabalho de TI para locadora de veiculos.

Plataforma web da **AutoVia**, locadora que aluga e vende veículos da própria frota.
Trabalho Interdisciplinar: Aplicações para Processos de Negócio — PUC Minas, 2026.

| Pasta | O que é | Como começar |
|---|---|---|
| [`backend/`](backend/README.md) | API REST em ASP.NET Core (.NET 10 LTS) + EF Core + PostgreSQL | `cd backend` → `dotnet tool restore` → `dotnet run --project AutoVia.Api --launch-profile http` |
| [`frontend/`](frontend/README.md) | React 18 + TypeScript + Vite + Tailwind | `cd frontend` → `npm install` → `npm run dev` |

- API: <http://localhost:5000> (Swagger em `/swagger`)
- Frontend: <http://localhost:5173>

O passo a passo completo para subir banco, backend e frontend (tarefa T-QA-02) será fechado ao final do projeto.
Convenções, mapa de pastas e regras de cada lado estão nos READMEs de `backend/` e `frontend/`.

Branches e commits citam o código da tarefa do plano (ex.: `feature/P4-FE-02-vistoria`, `T-BE-01: ...`).
