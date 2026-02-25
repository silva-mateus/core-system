# Migração: musicas-igreja -> Core System

## Visão Geral

O musicas-igreja foi a base para o Core System. A migração envolve:
1. Adicionar o core como submodule
2. Substituir auth local por Core.Auth
3. Substituir FileService por Core.FileManagement
4. Migrar SQLite para MySQL
5. Atualizar frontend para usar componentes do core

## Backend

### 1. Adicionar submodule

```bash
cd musicas-igreja
git submodule add <core-system-url> core
```

### 2. Atualizar .csproj

```xml
<!-- MusicasIgreja.Api.csproj -->
<!-- Remover: BCrypt.Net-Next (já está no Core.Auth) -->
<!-- Remover: Microsoft.Data.Sqlite (migrar para MySQL) -->

<!-- Adicionar: -->
<ProjectReference Include="../../core/backend/src/Core.Auth/Core.Auth.csproj" />
<ProjectReference Include="../../core/backend/src/Core.FileManagement/Core.FileManagement.csproj" />
<ProjectReference Include="../../core/backend/src/Core.Infrastructure/Core.Infrastructure.csproj" />
```

### 3. Definir permissões do projeto

Criar `Permissions.cs`:

```csharp
public static class Permissions
{
    public const string ViewMusic = "music:view";
    public const string DownloadMusic = "music:download";
    public const string EditMetadata = "music:edit_metadata";
    public const string UploadMusic = "music:upload";
    public const string DeleteMusic = "music:delete";
    public const string ManageLists = "lists:manage";
    public const string ManageCategories = "categories:manage";
    public const string ManageUsers = "users:manage";
    public const string ManageRoles = "roles:manage";
    public const string AccessAdmin = "admin:access";
}
```

### 4. Atualizar Program.cs

```csharp
// Substituir toda a configuração manual de session, auth, etc.
builder.Services.AddCoreDatabase<AppDbContext>(
    builder.Configuration.GetConnectionString("DefaultConnection")!
);

builder.Services.AddCoreAuth(options =>
{
    options.CookieName = ".MusicasIgreja.Session";
    options.DefaultRoles = new()
    {
        ["viewer"]   = [Permissions.ViewMusic, Permissions.DownloadMusic],
        ["editor"]   = [Permissions.ViewMusic, Permissions.DownloadMusic, Permissions.EditMetadata, Permissions.ManageLists, Permissions.ManageCategories],
        ["uploader"] = [Permissions.ViewMusic, Permissions.DownloadMusic, Permissions.EditMetadata, Permissions.UploadMusic, Permissions.ManageLists, Permissions.ManageCategories],
        ["admin"]    = [Permissions.ViewMusic, Permissions.DownloadMusic, Permissions.EditMetadata, Permissions.UploadMusic, Permissions.DeleteMusic, Permissions.ManageLists, Permissions.ManageCategories, Permissions.ManageUsers, Permissions.ManageRoles, Permissions.AccessAdmin]
    };
});

builder.Services.AddCoreFileManagement(options =>
{
    options.StoragePath = "./organized";
    options.AllowedExtensions = [".pdf"];
    options.OrganizeByCategory = true;
});

// ...

app.UseCoreAuth();
await app.Services.SeedCoreAuthAsync();
```

### 5. Atualizar AppDbContext

```csharp
protected override void OnModelCreating(ModelBuilder builder)
{
    builder.ApplyCoreAuthEntities();
    builder.ApplyCoreFileEntities();

    // Entidades específicas do musicas-igreja (Category, LiturgicalTime, MergeList, etc.)
    // ...
}
```

### 6. Substituir AuthController

O `CoreAuthController` já fornece: login, logout, me, change-password, profile.
Remover o `AuthController.cs` local e herdar ou usar diretamente.

### 7. Atualizar controllers para usar CoreAuthHelper

```csharp
// Antes:
var roleId = AuthHelper.GetCurrentRoleId(HttpContext);
var role = await _authService.GetRoleByIdAsync(roleId.Value);
if (!role.CanUploadMusic) return Forbid();

// Depois:
var check = await CoreAuthHelper.CheckPermissionAsync(HttpContext, _authService, Permissions.UploadMusic);
if (check is not null) return check;
```

### 8. Migrar SQLite para MySQL

```bash
# Atualizar appsettings.json
# De:
"Database": { "Path": "data/musicas.db" }
# Para:
"ConnectionStrings": {
  "DefaultConnection": "Server=mysql;Port=3306;Database=musicas_igreja;User=musicas_user;Password=xxx;CharSet=utf8mb4"
}
```

## Frontend

### 1. Atualizar tsconfig.json

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

### 2. Substituir componentes locais

```tsx
// Antes:
import { Button } from "@/components/ui/button"
// Depois:
import { Button } from "@core/components/ui/button"
```

### 3. Substituir AuthContext local

```tsx
// Antes:
import { useAuth } from "@/contexts/AuthContext"
const { canUploadMusic } = useAuth()

// Depois:
import { useAuth } from "@core/contexts/auth-context"
const { hasPermission } = useAuth()
const canUploadMusic = hasPermission("music:upload")
```

### 4. Substituir API client

```tsx
// Antes:
import { musicApi } from "@/lib/api"
// Depois:
import { useAuth } from "@core/contexts/auth-context"
const { api } = useAuth()
// ou criar wrappers locais que usam o api client do core
```
