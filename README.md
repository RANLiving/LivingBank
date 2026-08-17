# LivingBank

Aplicação que liga ao [Enable Banking](https://enablebanking.com) para ler saldos e movimentos das
contas bancárias configuradas, guarda tudo em base de dados por conta, e disponibiliza a informação
em web e app Android (APK), com multiutilizador e permissões por role.

## Arquitetura

```
LivingBank/
  backend/LivingBank.Api/   ASP.NET Core 9 Web API (C#) + EF Core + PostgreSQL
  frontend/                 React + Vite + TypeScript, Capacitor (gera o APK Android)
  .github/workflows/        Cron externo (GitHub Actions) que aciona a leitura Enable Banking
  render.yaml                Config de deploy do backend no Render (Docker)
```

- **Auth**: JWT + ASP.NET Identity, roles `Admin` / `Manager` / `Viewer`, permissões por policy.
- **Cron interno**: Quartz.NET corre a cada 5 min e compara com os 4 horários configurados em
  `SyncSchedule` (editável no ecrã "Agendamento"). Corre dentro do próprio serviço.
- **Cron externo**: como o plano free do Render adormece o serviço por inatividade, o workflow
  `.github/workflows/enable-banking-cron.yml` chama `POST /api/sync/external-trigger` 4x/dia para
  acordar o serviço e garantir que a leitura acontece mesmo se o Quartz interno não correu.
- **Limite diário**: `EnableBanking:MaxDailySyncsPerAccount` (default 4) é validado tanto na leitura
  agendada como na leitura forçada manual (`POST /api/sync/force/{bankAccountId}` devolve 429 se excedido).
- **Logs**: `AuditLog` regista todas as operações de escrita (quem, quê, quando, IP); `ErrorLog`
  regista exceções não tratadas e alimenta o ecrã de erros (`/logs`).
- **Fingerprint**: no APK, `capacitor-native-biometric` guarda as credenciais no keystore do
  dispositivo após o primeiro login e permite reentrar com biometria.

## Correr localmente

### Backend

Requisitos: .NET 9 SDK, PostgreSQL local (ou usa uma instância Neon grátis).

```bash
cd backend/LivingBank.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Database=livingbank;Username=postgres;Password=postgres"
dotnet user-secrets set "Jwt:Secret" "<gera uma string aleatória longa>"
dotnet user-secrets set "EnableBanking:ApplicationId" "<application id do Enable Banking>"
dotnet user-secrets set "EnableBanking:PrivateKeyPem" "<conteúdo da tua chave privada .pem>"
dotnet user-secrets set "EnableBanking:ExternalCronSecret" "<segredo à tua escolha>"
dotnet user-secrets set "Seed:AdminUserName" "admin"
dotnet user-secrets set "Seed:AdminEmail" "admin@livingtours.com"
dotnet user-secrets set "Seed:AdminPassword" "<palavra-passe forte>"
dotnet run
```

As migrações EF Core correm automaticamente no arranque. O utilizador admin só é criado se ainda
não existir nenhum utilizador e as chaves `Seed:*` estiverem definidas.

### Frontend

```bash
cd frontend
cp .env.example .env   # ajusta VITE_API_URL se necessário
npm install
npm run dev
```

### Gerar o APK

```bash
cd frontend
npm run build
npx cap sync android
npx cap open android    # abre no Android Studio para assinar e gerar o APK/AAB
```

O APK gerado não precisa de loja — pode ser distribuído diretamente (ex: GitHub Releases) e
instalado com "fontes desconhecidas" ativado no Android.

## Deploy gratuito

| Componente | Serviço | Notas |
|---|---|---|
| Frontend (web) | [Vercel](https://vercel.com) | `frontend/vercel.json` já configurado (SPA rewrites) |
| Backend (API) | [Render](https://render.com) | `render.yaml` na raiz, build via Docker (`backend/LivingBank.Api/Dockerfile`) |
| Base de dados | [Neon](https://neon.tech) | PostgreSQL grátis, copiar a connection string para `ConnectionStrings__Default` |
| Cron externo | [GitHub Actions](https://github.com/features/actions) | `.github/workflows/enable-banking-cron.yml`, grátis |

Nenhum destes serviços pede cartão de crédito no tier grátis usado aqui.

### Passos

1. **Neon**: cria um projeto Postgres, copia a connection string (formato `Host=...;Database=...;Username=...;Password=...`).
2. **Render**: liga o repositório, o `render.yaml` cria o serviço automaticamente. Preenche as
   variáveis marcadas `sync: false` no painel do Render (connection string, `Jwt__Secret`,
   credenciais Enable Banking, `Seed__*`, `Cors__AllowedOrigins__0` com o domínio Vercel).
3. **Vercel**: importa a pasta `frontend/`, define `VITE_API_URL` com o URL do serviço no Render.
4. **GitHub Actions**: em Settings → Secrets do repositório, define:
   - `LIVINGBANK_API_URL` — URL do backend no Render
   - `LIVINGBANK_CRON_SECRET` — o mesmo valor de `EnableBanking__ExternalCronSecret`

   Os 4 horários no workflow (`cron:`) devem refletir os horários definidos no ecrã "Agendamento";
   se os alterares lá, atualiza também o ficheiro `.github/workflows/enable-banking-cron.yml`.

## Permissões por role

| Ação | Admin | Manager | Viewer |
|---|:---:|:---:|:---:|
| Ver contas/saldos/movimentos | ✅ | ✅ | ✅ |
| Forçar leitura Enable Banking | ✅ | ✅ | ❌ |
| Criar/gerir contas bancárias | ✅ | ✅ | ❌ |
| Gerir utilizadores | ✅ | ❌ | ❌ |
| Ver logs (auditoria + erros) | ✅ | ❌ | ❌ |

## Notas de segurança

- Dados bancários são sensíveis (PSD2/GDPR): nunca commitar `appsettings.Development.json` nem
  chaves reais — usa `dotnet user-secrets` local e variáveis de ambiente em produção (já cobertas
  pelo `.gitignore`).
- A chave privada do Enable Banking (`EnableBanking:PrivateKeyPem`) só deve existir como variável
  de ambiente no Render, nunca no repositório.
- O free tier serve para desenvolvimento/validação; para produção com clientes reais, considerar
  planos pagos com SLA e backups garantidos.
