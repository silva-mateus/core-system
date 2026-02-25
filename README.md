# Core System

Módulos compartilhados reutilizáveis para projetos pessoais, cobrindo backend (.NET 9) e frontend (Next.js).

## Estrutura

```
core-system/
├── backend/
│   ├── CoreSystem.sln
│   └── src/
│       ├── Core.Common/          # Entidades base, Result<T>, extensions
│       ├── Core.Auth/            # Autenticação session-based, RBAC, rate limiting
│       ├── Core.FileManagement/  # Upload, storage, deduplicação de arquivos
│       └── Core.Infrastructure/  # EF Core + MySQL (Pomelo), entity configs
└── frontend/
    ├── components/ui/            # Componentes shadcn/ui unificados
    ├── contexts/                 # CoreAuthProvider
    ├── hooks/                    # use-mobile, use-toast, use-debounce
    ├── lib/                      # cn, format, storage, array, api-client
    └── types/
```

## Integração em um Projeto

### 1. Adicionar como Git Submodule

```bash
cd meu-projeto
git submodule add <url-do-core-system> core
git submodule update --init --recursive
```

### 2. Backend (.NET)

Adicionar referências no `.csproj` do projeto:

```xml
<ItemGroup>
  <ProjectReference Include="../../core/backend/src/Core.Auth/Core.Auth.csproj" />
  <ProjectReference Include="../../core/backend/src/Core.FileManagement/Core.FileManagement.csproj" />
  <ProjectReference Include="../../core/backend/src/Core.Infrastructure/Core.Infrastructure.csproj" />
</ItemGroup>
```

No `Program.cs`:

```csharp
using Core.Auth.Extensions;
using Core.FileManagement.Extensions;
using Core.Infrastructure.Extensions;

// Database
builder.Services.AddCoreDatabase<AppDbContext>(
    builder.Configuration.GetConnectionString("DefaultConnection")!
);

// Auth
builder.Services.AddCoreAuth(options =>
{
    options.CookieName = ".MeuProjeto.Session";
    options.DefaultRoles = new()
    {
        ["viewer"] = ["music:view", "music:download"],
        ["editor"] = ["music:view", "music:download", "music:edit"],
        ["admin"]  = ["music:view", "music:download", "music:edit", "music:upload", "music:delete", "users:manage"]
    };
});

// File Management (opcional)
builder.Services.AddCoreFileManagement(options =>
{
    options.StoragePath = "./uploads";
    options.AllowedExtensions = [".pdf"];
});

var app = builder.Build();

app.UseCoreAuth();  // Session + AuditMiddleware

// Seed roles on startup
await app.Services.SeedCoreAuthAsync();
```

No `AppDbContext`:

```csharp
using Core.Infrastructure.Extensions;

public class AppDbContext : DbContext
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyCoreAuthEntities();   // core_users, core_roles, core_role_permissions
        builder.ApplyCoreFileEntities();   // core_stored_files

        // Entidades do projeto...
    }
}
```

### 3. Frontend (Next.js)

No `tsconfig.json`:

```json
{
  "compilerOptions": {
    "paths": {
      "@/*": ["./src/*"],
      "@core/*": ["./core/frontend/*"]
    }
  }
}
```

No `next.config.js`:

```js
const nextConfig = {
  transpilePackages: ['../core/frontend'],
};
export default nextConfig;
```

Uso nos componentes:

```tsx
import { Button } from "@core/components/ui/button"
import { useAuth, CoreAuthProvider } from "@core/contexts/auth-context"
import { formatDate } from "@core/lib/format"
import { useDebounce } from "@core/hooks/use-debounce"
```

No layout root:

```tsx
import { CoreAuthProvider } from "@core/contexts/auth-context"

export default function RootLayout({ children }) {
  return (
    <CoreAuthProvider config={{ apiBasePath: "/api" }}>
      {children}
    </CoreAuthProvider>
  )
}
```

## Permissões

O sistema de permissões usa strings livres. Cada projeto define suas próprias:

```csharp
// Backend
public static class Permissions
{
    public const string ViewMusic = "music:view";
    public const string UploadMusic = "music:upload";
}

// No controller
var check = await CoreAuthHelper.CheckPermissionAsync(HttpContext, _authService, Permissions.UploadMusic);
if (check is not null) return check;
```

```tsx
// Frontend
const { hasPermission } = useAuth()
if (hasPermission("music:upload")) { /* show upload button */ }
```

## CI/CD

No GitHub Actions do projeto, use o workflow reutilizável do `homelab-infra`:

```yaml
name: Deploy
on:
  push:
    branches: [main]
jobs:
  deploy-api:
    uses: SEU_USER/homelab-infra/.github/workflows/reusable-deploy.yml@main
    with:
      service_name: musicas-igreja-api
      dockerfile_path: backend/Dockerfile
      context: .
    secrets: inherit
```

O checkout usa `submodules: recursive` para incluir o core no build.
