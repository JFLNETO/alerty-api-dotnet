# 🚀 Alerty API (.NET)

Backend do Alerty, um SaaS de gestão de mensalidades e alertas automáticos para academias, CTs e serviços recorrentes.

O projeto original foi desenvolvido utilizando Bubble.io + n8n + PostgreSQL.  
Esta versão representa a recriação do backend em código utilizando ASP.NET Core e PostgreSQL.

---

# 📌 Objetivo

Migrar gradualmente o Alerty de uma arquitetura low-code/no-code para uma arquitetura moderna baseada em:

- ASP.NET Core
- PostgreSQL
- Entity Framework Core
- React + TypeScript (em desenvolvimento)

---

# 🛠 Tecnologias utilizadas

- ASP.NET Core Minimal API
- Entity Framework Core
- PostgreSQL
- Swagger
- Dependency Injection
- DTO Pattern
- Service Layer Pattern

---

# 🏗 Arquitetura atual

```txt
Alerty.Api
│
├── Data
├── DTOs
├── Endpoints
├── Models
├── Services
```

---

# ✅ Funcionalidades implementadas

- Cadastro de clientes
- Edição de clientes
- Ativar/Inativar cliente
- Confirmação de pagamento
- Histórico de pagamentos
- Relatórios financeiros
- Filtros de clientes
- Integração com PostgreSQL
- Swagger/OpenAPI

---

# 📄 Endpoints principais

## Clientes

```http
GET /clientes
GET /clientes/{id}
POST /clientes
PUT /clientes/{id}
PATCH /clientes/{id}/status
```

## Pagamentos

```http
POST /pagamentos/confirmar
```

## Relatórios

```http
GET /relatorios/pagamentos
```

---

# 📦 Como executar o projeto

## Clone o repositório

```bash
git clone https://github.com/JFLNETO/alerty-api-dotnet.git
```

## Entre na pasta

```bash
cd alerty-api-dotnet
```

## Configure a connection string

No arquivo:

```txt
appsettings.Development.json
```

Configure:

```json
"ConnectionStrings": {
  "DefaultConnection": "SUA_CONNECTION_STRING"
}
```

## Execute a aplicação

```bash
dotnet run
```

---

# 📘 Swagger

Após executar o projeto:

```txt
http://localhost:5069/swagger
```

---

# 🧠 Aprendizados aplicados neste projeto

- Arquitetura em camadas
- Minimal APIs
- DTOs
- Services
- Organização de backend .NET
- Integração PostgreSQL + EF Core
- Tratamento de erros
- Estruturação de APIs REST

---

# 🚧 Próximos passos

- Front-end em React + TypeScript
- Autenticação JWT
- Multiempresa
- Jobs automáticos
- Integração WhatsApp
- Deploy em VPS Linux

---

# 👨‍💻 Autor

Desenvolvido por José Ferreira Lima Neto.

- Bubble.io Developer
- Backend Developer (.NET em evolução)
- PostgreSQL / APIs / n8n
