# Cursor Rules Templates

Regras padronizadas para manter o mesmo padrão de AI agents entre todos os projetos.

## Arquivos

| Arquivo | Scope | Descrição |
|---------|-------|-----------|
| `core-standards.mdc` | `alwaysApply: true` | Idioma, naming, commits, arquitetura base |
| `backend-dotnet.mdc` | `backend/**/*.cs` | .NET, controllers, services, EF Core, logging |
| `frontend-nextjs.mdc` | `frontend/**/*.ts,tsx` | Next.js, shadcn/ui, forms, styling |
| `testing.mdc` | `**/*test*,**/*Test*` | xUnit, Vitest, naming, patterns |
| `security-auth.mdc` | `**/*auth*,**/*Auth*` | Core.Auth, permissões, segurança |

## Como usar em um projeto

1. Copie os arquivos para `.cursor/rules/` do projeto:

```bash
cp -r core/cursor-rules/*.mdc .cursor/rules/
```

2. Adicione regras específicas do projeto (ex: `project.mdc`):

```markdown
---
alwaysApply: true
---

# Projeto: Musicas Igreja

## Domínio
Sistema de gerenciamento de partituras e repertórios para igrejas.

## Permissões
- `music:view`, `music:download`, `music:edit_metadata`
- `music:upload`, `music:delete`
- `lists:manage`, `categories:manage`
- `users:manage`, `roles:manage`, `admin:access`

## Estrutura
- `backend/` -- .NET 9 API
- `frontend/` -- Next.js 14
- `core/` -- git submodule (core-system)
```

3. Ajuste os `globs` se a estrutura do projeto for diferente.
