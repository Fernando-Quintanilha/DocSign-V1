# HoleriteSign — Design Plan Document

> **Version:** 2.4  
> **Date:** February 27, 2026  
> **Status:** Draft  

---

## 1. Executive Summary

**HoleriteSign** is a SaaS platform that solves the painful process of collecting employee signatures on payslips (holerites) for companies using PontoTel. It replaces paper-based or complex digital workflows with a streamlined system where:

1. An **admin** uploads monthly payslip PDFs per employee.  
2. The system **automatically notifies** each employee via WhatsApp and/or email with a unique, one-time link.  
3. The employee opens the link (no login required), **views their payslip**, takes a **selfie photo** as their digital signature, and submits.  
4. The signed PDF (with photo + full audit trail embedded) is stored securely and available for the admin to download.

The platform must comply with **Brazilian Law 14.063/2020**, **Provisional Measure 2.200-2/2001**, and the **LGPD** (Lei Geral de Proteção de Dados).

---

## 2. Personas & User Flows

### 2.1 Personas

| Persona | Description |
|---------|-------------|
| **Super Admin (Dev/Operator)** | System operator (you). Full access to all accounts, can change plans, deactivate accounts, view global metrics. First account created via DB seed. |
| **Admin (Client)** | HR/Payroll manager of a company. Self-registers, registers employees, uploads payslips, monitors signature status. Accesses the system via authenticated admin panel. |
| **Employee (Signer)** | CLT employee. Receives a link via WhatsApp/email, views payslip PDF, signs with selfie. **No login, no account.** |

### 2.2 User Flow — Super Admin (Dev/Operator)

```
Login (seeded account) → Super Admin Dashboard
  → View all accounts (clients) + global metrics
  → Change plan for any account
  → Activate / Deactivate accounts
  → Manage plan tiers (create, edit limits)
  → Impersonate admin (view their panel for debugging)
  → View global audit logs
```

### 2.3 User Flow — Admin

```
Login → Dashboard → Manage Employees (CRUD)
                   → Upload Payslips (per employee, per month)
                   → Send Notifications (WhatsApp / Email)
                   → Monitor Signature Status
                   → Download Signed PDFs + Audit Logs
```

### 2.3 User Flow — Employee (Signer)

```
Receive link (WhatsApp/Email)
  → Open link in browser (no login)
    → View payslip PDF
      → Take selfie photo (camera capture)
        → Confirm & submit
          → Success page ("Assinatura registrada com sucesso")
```

---

## 3. Functional Requirements

### 3.1 Authentication & Authorization

| ID | Requirement | Priority |
|----|-------------|----------|
| AUTH-01 | Admin login with email + password | Must |
| AUTH-02 | **Self-registration** — admin creates own account (name, email, company, password). Automatically assigned Free plan | Must |
| AUTH-03 | Email verification on registration (confirm email before full access) | Must |
| AUTH-04 | Password reset via email | Must |
| AUTH-05 | Session management with JWT (access + refresh tokens) | Must |
| AUTH-06 | Employee access via unique, time-limited, single-use token (no login) | Must |
| AUTH-07 | Token expiration configurable by admin (default: 7 days) | Should |
| AUTH-08 | **Role-based access control** — two roles: `admin` (client) and `superadmin` (dev/operator). Same panel, extra sections visible to superadmin | Must |
| AUTH-09 | First superadmin account created via **database seed** (migration), not via public registration | Must |
| AUTH-10 | Superadmin can create other superadmin accounts | Should |

### 3.2 Employee Management

| ID | Requirement | Priority |
|----|-------------|----------|
| EMP-01 | CRUD operations for employees (name, WhatsApp, email, CPF, date of birth) | Must |
| EMP-02 | Bulk import of employees via CSV/Excel (including CPF and birth date columns) | Should |
| EMP-03 | Search and filter employees by name | Must |
| EMP-04 | Employee cards in admin panel showing status per month | Must |
| EMP-05 | Soft delete (deactivate) employees — employee remains visible in past months' history but is excluded from new pay periods going forward | Must |
| EMP-06 | Employees persist across months — no need to re-register every month; the same employee roster carries over automatically to new pay periods | Must |
| EMP-07 | Historical integrity — removing/deactivating an employee does NOT affect documents or signatures from previous months; those records are immutable | Must |

### 3.3 Document Management

| ID | Requirement | Priority |
|----|-------------|----------|
| DOC-01 | Upload payslip PDF per employee per month | Must |
| DOC-02 | Organize documents by month/year (e.g., "2026-02") | Must |
| DOC-03 | Batch upload of multiple PDFs with auto-assignment by filename pattern | Should |
| DOC-04 | Preview uploaded PDF in admin panel | Must |
| DOC-05 | Replace/re-upload a PDF before it is signed | Must |
| DOC-06 | Download original and signed versions of PDF | Must |
| DOC-07 | **Token expiration job** — a daily Hangfire job marks documents with status `sent` and `token_expires_at < NOW()` as `expired`, and logs an audit event `token.expired` for each. This ensures the status enum stays accurate without manual intervention | Should |

### 3.4 Notification System

| ID | Requirement | Priority |
|----|-------------|----------|
| NOT-01 | Send signature request via **WhatsApp** (primary channel — preferred by employees) | Must |
| NOT-02 | Send signature request via email (secondary/fallback channel) | Must |
| NOT-03 | Bulk send notifications for all pending employees of a given month | Must |
| NOT-04 | Resend notification for individual employee | Must |
| NOT-05 | Notification templates configurable by admin | Should |
| NOT-06 | Delivery status tracking (sent, delivered, read — where available) | Should |

> **Nota:** WhatsApp é o canal principal. A maioria dos funcionários vai receber e abrir o link pelo WhatsApp, por ser mais usual no dia a dia. Email serve como fallback.

### 3.5 Signature Flow (Employee-Facing)

| ID | Requirement | Priority |
|----|-------------|----------|
| SIG-01 | Responsive web page accessible via unique link | Must |
| SIG-02 | Display payslip PDF inline (embedded viewer) | Must |
| SIG-03 | Selfie capture via device camera (front-facing) | Must |
| SIG-04 | Photo preview before confirmation | Must |
| SIG-05 | Single-tap confirmation to submit signature | Must |
| SIG-06 | Collect device metadata at signature time (IP, User-Agent, timestamp, geolocation if permitted) | Must |
| SIG-07 | Show confirmation page after successful signature | Must |
| SIG-08 | Prevent re-signing (link becomes invalid after signature) | Must |
| SIG-09 | Link expiration with friendly error page | Must |
| SIG-10 | Option for employee to download their signed PDF after completing the signature | Low |
| SIG-11 | **Identity verification via CPF or date of birth** — employee must verify identity by entering CPF or date of birth (must match employee record) before signature is accepted. Zero-cost, no external API dependency | Must |
| SIG-12 | **OTP verification via WhatsApp (optional upgrade)** — if enabled by admin, send a 6-digit OTP to the employee's registered WhatsApp before accepting signature. Rules: max 3 attempts per 10 min, 60s cooldown between resends, OTP expires in 5 min, one OTP per token (not per session) | Should |
| SIG-13 | **Confirmation checkbox + minimum viewing time** — employee must check a checkbox ("Li e concordo com o documento") and spend a minimum time on the PDF page (configurable, default 10s) before the sign button is enabled. Log `pdf.opened` event on page load | Must |
| SIG-14 | **Scroll tracking** — optionally track scroll-to-bottom on the PDF viewer. Not required to enable signing, but logged as `pdf.view.completed` event for audit evidence | Should |
| SIG-15 | **Camera fallback (gallery upload)** — if device camera access fails or is denied, allow the employee to upload a photo from their gallery as an alternative | Should |
| SIG-16 | **Dedicated error pages** — specific, friendly pages for: token expired, already signed, document not found, and generic error. Each with clear messaging in Portuguese | Must |
| SIG-17 | **Photo upload retry & progress bar** — if selfie upload fails, allow retry without restarting the flow. Show upload progress bar during submission | Should |

### 3.6 Signed Document Generation

> **Data Model Notes (v2.1):**
> - **DATA-01** — Status enum simplified to `uploaded → sent → signed → expired`. The `viewed` status is removed; viewing is tracked via the `viewed_at` timestamp column on the `documents` table instead. This avoids ambiguity (a document can be "viewed" and "sent" simultaneously).
> - **DATA-02** — Two new derived-timestamp columns added to `documents`: `viewed_at` (when the employee first opened the signing page) and `last_notified_at` (when the last notification was dispatched). These support dashboard filters and analytics without querying the audit log.

| ID | Requirement | Priority |
|----|-------------|----------|
| PDF-01 | Append signature page to original PDF with: selfie photo, full name, timestamp, IP address, device info | Must |
| PDF-02 | Generate SHA-256 hash of the original PDF and embed in signed version | Must |
| PDF-03 | Generate SHA-256 hash of the selfie and embed in signed version | Must |
| PDF-04 | Full audit log page appended to signed PDF | Must |
| PDF-05 | Digital seal/watermark on each page of signed PDF indicating "Assinado digitalmente via HoleriteSign" | Should |

### 3.7 Audit Trail

| ID | Requirement | Priority |
|----|-------------|----------|
| AUD-01 | Log every event: document upload, link generation, link access, photo capture, signature submission | Must |
| AUD-02 | Store IP address, User-Agent, timestamp (UTC), and geolocation (if available) for every event | Must |
| AUD-03 | Immutable audit log (append-only, no edits or deletes) | Must |
| AUD-04 | Export audit trail as PDF or JSON | Should |
| AUD-05 | Admin dashboard showing signature timeline per document | Must |

### 3.8 Admin Dashboard

| ID | Requirement | Priority |
|----|-------------|----------|
| DASH-01 | Overview: total employees, pending signatures, completed signatures per month | Must |
| DASH-02 | Filter by month/year | Must |
| DASH-03 | Status indicators per employee: pending, sent, signed | Must |
| DASH-04 | Quick actions: send reminder, download signed PDF, view audit log | Must |
| DASH-05 | Activity log / recent events feed | Should |
| DASH-06 | **"Quem ainda não assinou"** — prominent list/section showing employees with pending signatures for the selected month, with one-click reminder button | Must |
| DASH-07 | Visual progress bar per month (e.g., "32/50 assinaturas concluídas") | Must |

### 3.9 Backup & Data Export

| ID | Requirement | Priority |
|----|-------------|----------|
| BAK-01 | Bulk download: export all signed PDFs for a given month as a ZIP file | Must |
| BAK-02 | Bulk download: export ALL data (all months, all documents, all audit logs) as a ZIP | Must |
| BAK-03 | Export employee list as CSV | Should |
| BAK-04 | Export audit logs as JSON or CSV | Should |
| BAK-05 | Scheduled automatic backup option (weekly/monthly) | Low |

### 3.10 Plan & Usage Limits

| ID | Requirement | Priority |
|----|-------------|----------|
| PLAN-01 | **Self-registration** — new admins create their own account and receive the Free plan automatically (10 docs/mês, 5 funcionários) | Must |
| PLAN-02 | Each account has a plan with a monthly document signature limit (e.g., Free: 10, Basic: 50, Pro: 200, Enterprise: unlimited) | Must |
| PLAN-03 | System automatically enforces the document limit — blocks upload/send when limit is reached | Must |
| PLAN-04 | Dashboard shows current usage vs. plan limit (e.g., "42/50 documentos usados este mês") | Must |
| PLAN-05 | Plan upgrade page — admin can see available plans and request upgrade | Must |
| PLAN-06 | System operator (super admin) can change plan/limit for any account without code changes | Must |
| PLAN-07 | **Limit counts documents sent, not uploaded** — usage is counted when a notification is sent (status → 'sent'), not when a PDF is uploaded. This allows admins to prepare documents before committing to their quota. **Resend does not count again** — each document is counted at most once per period; resending only updates `last_notified_at` and creates a new `notifications` row but does not increment usage | Must |
| PLAN-08 | **Block send, not upload** — when the plan limit is reached, the admin can still upload PDFs but cannot send notifications. Show a clear upgrade CTA: "Você atingiu o limite do seu plano. Faça upgrade para continuar enviando" | Must |

---

## 4. Non-Functional Requirements

### 4.1 Security & Compliance

| ID | Requirement | Details |
|----|-------------|---------|
| SEC-01 | **Law 14.063/2020 compliance** | System must qualify as "assinatura eletrônica avançada" (advanced electronic signature). Requires: unique association with the signer, signer identification at time of signing, use of data under the signer's exclusive control, detection of any subsequent modification. |
| SEC-02 | **MP 2.200-2/2001 compliance** | Electronic documents signed outside ICP-Brasil are valid when accepted by the parties or by the person to whom the document is presented. The selfie + metadata approach satisfies this for private-party agreements. |
| SEC-03 | **LGPD compliance** | Collect only necessary personal data. Obtain consent at time of signature. Provide data subject rights (access, deletion requests). Encrypt PII at rest and in transit. Define data retention policy. Appoint DPO contact. |
| SEC-04 | **HTTPS everywhere** | All traffic over TLS 1.2+ |
| SEC-05 | **Encryption at rest** | AES-256 for stored documents and photos |
| SEC-06 | **Encryption in transit** | TLS 1.2+ for all API calls |
| SEC-07 | **Access tokens** | Cryptographically random, single-use, time-limited tokens for employee links |
| SEC-08 | **Rate limiting** | Prevent brute-force on admin login and token endpoints |
| SEC-09 | **Input validation** | Server-side validation for all inputs; sanitize file uploads |
| SEC-10 | **CORS policy** | Restrict to known origins |
| SEC-11 | **Tenant isolation (Data Leak Prevention)** | Use EF Core Global Query Filters to automatically scope all queries by `AdminId`. Prevents one admin from ever seeing another company's data, even if a `WHERE` clause is missing in code. Example: `builder.Entity<Employee>().HasQueryFilter(e => e.AdminId == _currentTenantService.AdminId);` |
| SEC-12 | **Super Admin seed security** | Never hardcode password/hash in source code. Use environment variables at application startup to generate the initial super admin hash. Keep the repository clean and credential-free |
| SEC-13 | **Presigned URLs for uploads** | Instead of proxying files through the API (memory/bandwidth cost), generate S3/MinIO Presigned URLs. Frontend uploads PDFs and selfies directly to storage. API only handles business logic + records metadata |
| SEC-14 | **Audit log DB-level immutability** | Create a dedicated database user for the API that has `INSERT` + `SELECT` only on `audit_logs` (no `UPDATE`, no `DELETE`). Even if the API is compromised, the audit trail cannot be tampered with. Critical for MP 2.200-2/2001 and LGPD compliance in court |
| SEC-15 | **Token hashing** | Store only the SHA-256 hash of signing tokens in the database (`signing_token_hash`), never the raw token. The raw token is sent to the employee via notification link. On access, the API hashes the incoming token and looks up the hash. If the DB is compromised, tokens cannot be reconstructed |
| SEC-16 | **Audit log hash chain** | Each audit log entry stores a `prev_hash` (hash of the previous entry in the same chain) and its own `entry_hash` (SHA-256 of `id + event_type + event_data + created_at + prev_hash`). **Chain is scoped per `document_id`** (or per `admin_id` for non-document events) to avoid global serialization bottlenecks. This is tamper-evident, not absolute proof without external anchoring. Optional future enhancement: daily checkpoint hash emailed to superadmin. Include `chain_version` for future algorithm upgrades |
| SEC-17 | **Impersonation guardrails** | Super admin impersonation tokens are time-limited (max 15 minutes), read-only (no write operations), fully audit-logged (`super.impersonate.started`, `super.impersonate.ended`), and display a prominent banner in the UI: "Você está visualizando como [Admin Name]" |
| SEC-18 | **Geolocation opt-in** | Geolocation collection during signing is opt-in only. Show a clear consent prompt in Portuguese: "Permitir acesso à localização para registro de assinatura?" Store consent decision in signature metadata. Never block signing if geolocation is denied |
| SEC-19 | **Idempotent signature submission** | The `POST /api/sign/{token}/submit` endpoint must be idempotent — if called twice with the same token, the second call returns the existing signature instead of creating a duplicate. Prevents double-signing from network retries or user double-clicks |

### 4.2 Performance

| Requirement | Target |  
|-------------|--------|
| Page load time (employee signing page) | < 3 seconds |
| PDF rendering time | < 2 seconds for PDFs up to 5MB |
| API response time (95th percentile) | < 500ms |
| Concurrent users supported | 500+ |
| PDF upload size limit | 10MB |

### 4.3 Scalability

- Stateless API servers (horizontally scalable)
- Cloud object storage for documents (S3-compatible)
- Database connection pooling
- CDN for static assets

### 4.4 Availability

- Target uptime: 99.5%
- Automated health checks
- Database backups: daily with 30-day retention

---

## 5. Technical Architecture

### 5.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────┐
│                      FRONTEND                           │
│              React + TypeScript + Vite                   │
│                                                         │
│  ┌─────────────────┐       ┌──────────────────────┐    │
│  │  Admin Panel     │       │  Signing Page         │    │
│  │  (SPA - Vite)    │       │  (SPA - public route) │    │
│  │  - Login         │       │  - PDF Viewer          │    │
│  │  - Dashboard     │       │  - Camera Capture      │    │
│  │  - Employee CRUD │       │  - Confirmation        │    │
│  │  - Doc Upload    │       │  - No auth required    │    │
│  │  - Notifications │       │  - Mobile-first        │    │
│  │  - Responsive    │       │                        │    │
│  └────────┬─────────┘       └───────────┬────────────┘    │
│           │                             │                │
└───────────┼─────────────────────────────┼────────────────┘
            │           HTTPS             │
┌───────────┼─────────────────────────────┼────────────────┐
│           ▼          BACKEND            ▼                │
│  ┌──────────────────────────────────────────────┐       │
│  │     ASP.NET Core 8 Web API (C#)               │       │
│  │                                                │       │
│  │  ┌──────────┐ ┌───────────┐ ┌──────────────┐ │       │
│  │  │ Auth     │ │ Employee  │ │ Document     │ │       │
│  │  │ Module   │ │ Module    │ │ Module       │ │       │
│  │  └──────────┘ └───────────┘ └──────────────┘ │       │
│  │  ┌──────────┐ ┌───────────┐ ┌──────────────┐ │       │
│  │  │Signature │ │Notification│ │ Audit        │ │       │
│  │  │ Module   │ │ Module    │ │ Module       │ │       │
│  │  └──────────┘ └───────────┘ └──────────────┘ │       │
│  │  ┌──────────┐ ┌───────────────────────────┐   │       │
│  │  │ Plans &  │ │  PDF Processing Service   │   │       │
│  │  │ Limits   │ │  (QuestPDF / iText7)      │   │       │
│  │  └──────────┘ └───────────────────────────┘   │       │
│  └──────────────────────────────────────────────┘       │
│                          │                               │
│          ┌───────────────┼───────────────┐               │
│          ▼               ▼               ▼               │
│  ┌──────────────┐ ┌───────────┐ ┌──────────────┐       │
│  │  PostgreSQL   │ │   S3 /    │ │   Redis      │       │
│  │  Database     │ │  MinIO    │ │   Cache      │       │
│  │  - Users      │ │  - PDFs   │ │  - Sessions  │       │
│  │  - Employees  │ │  - Photos │ │  - Rate Limit│       │
│  │  - Plans      │ │  - Signed │ │  - Background│       │
│  │  - Documents  │ │    docs   │ │    Jobs      │       │
│  │  - Audit Logs │ │           │ │              │       │
│  └──────────────┘ └───────────┘ └──────────────┘       │
│                                                         │
│          ┌───────────────────────────────┐               │
│          │    External Services          │               │
│          │  - WhatsApp Business API      │               │
│          │  - Email (SendGrid/SES)       │               │
│          │  - Geolocation API            │               │
│          └───────────────────────────────┘               │
└─────────────────────────────────────────────────────────┘
```

### 5.2 Tech Stack

| Layer | Technology | Justification |
|-------|-----------|---------------|
| **Frontend — Admin** | React 18 + TypeScript + Vite | SPA with fast HMR; responsive web (works on desktop + mobile browsers) |
| **Frontend — Signing Page** | Same React app, public route (`/assinar/:token`) | Mobile-first design; no auth on this route |
| **UI Framework** | Tailwind CSS + shadcn/ui | Rapid, consistent, professional UI |
| **Routing** | React Router v6 | Client-side routing with protected route wrappers |
| **State Management** | TanStack Query (React Query) + Zustand | Server state caching + lightweight client state |
| **Backend API** | **ASP.NET Core 8 (C#)** | High performance, strong typing, excellent for enterprise SaaS, great learning opportunity |
| **Database** | **PostgreSQL 16** | See § 5.2.1 — Database Comparison below |
| **ORM** | Entity Framework Core 8 | Type-safe, migrations, LINQ queries, mature C# ORM |
| **Object Storage** | AWS S3 (or MinIO for self-hosted) | Scalable, durable file storage for PDFs and photos |
| **Cache / Queue** | Redis | Session store, rate limiting, background job queues |
| **Background Jobs** | Hangfire (Redis-backed) | Background processing for notifications, PDF generation; built-in dashboard |
| **PDF Processing** | QuestPDF (generation) + react-pdf (frontend viewing) | QuestPDF is free, C#-native, fluent API for PDF creation |
| **WhatsApp API** | Meta WhatsApp Business Cloud API (or Z-API/Evolution API for Brazil) | Direct integration for messaging |
| **Email** | SendGrid or AWS SES | Reliable transactional email delivery |
| **Auth** | ASP.NET Core Identity + JWT Bearer tokens | Built-in identity management with BCrypt, integrated with the framework |
| **Hosting** | Vercel/Netlify (frontend SPA) + Railway/Azure (API) or AWS | Simplified deployment pipeline |
| **CI/CD** | GitHub Actions | Automated testing and deployment |

#### 5.2.1 Database Comparison — Why PostgreSQL

Você pediu uma orientação sobre qual SGBD escolher. Aqui está a análise de cada opção para o nosso caso de uso:

| SGBD | Tipo | Prós | Contras | Veredicto |
|------|------|------|---------|-----------|
| **PostgreSQL** | Relacional, open-source | ACID compliant; suporte nativo a JSON/JSONB (ótimo para audit logs); excelente para dados relacionais (funcionários ↔ documentos ↔ assinaturas); extensível; gratuito; excelente suporte com Entity Framework Core; pool de conexões maduro; índices avançados (GIN, GiST); criptografia at rest; amplamente adotado no mercado | Configuração inicial um pouco mais complexa que MySQL | ✅ **Escolhido** |
| **MySQL** | Relacional, open-source | Popular, fácil de configurar, boa performance para leitura | Suporte a JSON menos maduro que PostgreSQL; menos funcionalidades avançadas; problemas históricos com ACID em certos engines; EF Core support OK mas PostgreSQL é melhor | ⚠️ Serviria, mas PostgreSQL é superior para este caso |
| **MongoDB** | NoSQL, document-based | Flexível para dados sem esquema; fácil de começar | **Péssima escolha aqui** — nossos dados são altamente relacionais (admin → employees → documents → signatures → audit_logs). MongoDB não tem JOINs nativos, não garante ACID em multi-document por padrão, dificulta queries complexas. Para compliance legal, integridade referencial é essencial | ❌ Não recomendado |
| **Supabase** | PostgreSQL gerenciado + extras | É PostgreSQL por baixo (todas as vantagens); inclui auth, storage, realtime integrados; painel visual; API REST automática | Acoplamento com ecossistema Supabase; as features extras (auth, storage) competem com o que faremos no backend C#; vendor lock-in parcial; menos controle fino | ⚠️ Bom para prototipagem, mas como temos backend C# próprio, é melhor usar PostgreSQL puro |
| **Firebase (Firestore)** | NoSQL, Google Cloud | Tempo real, fácil para apps mobile, auth integrado | **Não recomendado** — mesmos problemas que MongoDB (NoSQL para dados relacionais); queries limitadas; vendor lock-in total com Google; custo pode escalar rápido; difícil de migrar; não ideal com backend C# | ❌ Não recomendado |

**Conclusão:** PostgreSQL é a escolha certa. Dados relacionais fortes, compliance exige integridade e auditoria, o suporte com C# (EF Core + Npgsql) é excelente, e temos JSONB para campos flexíveis como metadata.

#### 5.2.2 Backend — Why C# / ASP.NET Core

Análise da escolha do backend em C#:

| Aspecto | ASP.NET Core 8 (C#) |
|---------|---------------------|
| **Performance** | Um dos frameworks mais rápidos do mercado (benchmarks TechEmpower). Muito superior a Node.js/Express em throughput |
| **Tipagem** | Fortemente tipado — menos bugs em runtime, melhor refatoração, IntelliSense excelente |
| **Ecossistema** | Entity Framework Core (ORM maduro), Identity (auth built-in), Hangfire (background jobs), QuestPDF (geração de PDF), FluentValidation, AutoMapper |
| **Aprendizado** | C# é uma linguagem muito bem documentada. A Microsoft tem documentação de nível enterprise. Aprender C# abre portas para .NET, Unity, Azure, etc. |
| **Estrutura** | Clean Architecture natural com Controllers → Services → Repositories. Dependency Injection nativo |
| **PDF** | QuestPDF (gratuito, API fluente) ou iText7 — ambos nativos C#, muito mais poderosos que pdf-lib do Node.js |
| **Deploy** | Docker container, Azure App Service, Railway, AWS ECS — flexível |
| **Desvantagem** | Curva de aprendizado inicial se nunca usou C#, mas a estruturação do código é muito clara |

### 5.3 Database Schema

```sql
-- =====================================================
-- CORE TABLES
-- =====================================================

-- Plans (document signature limits)
CREATE TABLE plans (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name            VARCHAR(50) NOT NULL,          -- 'free', 'basic', 'pro', 'enterprise'
    display_name    VARCHAR(100) NOT NULL,         -- 'Plano Gratuito', 'Plano Básico', etc.
    max_documents   INTEGER NOT NULL DEFAULT 10,   -- Monthly document limit (-1 = unlimited)
    max_employees   INTEGER NOT NULL DEFAULT 5,    -- Max employees (-1 = unlimited)
    is_active       BOOLEAN DEFAULT TRUE,
    created_at      TIMESTAMPTZ DEFAULT NOW()
);

-- Seed default plans
INSERT INTO plans (name, display_name, max_documents, max_employees) VALUES
    ('free',       'Plano Gratuito',    10,   5),
    ('basic',      'Plano Básico',      50,  25),
    ('pro',        'Plano Profissional', 200, 100),
    ('enterprise', 'Plano Enterprise',   -1,  -1);

-- Admin users (the clients who manage the system)
-- Self-registration: admin creates own account, receives Free plan automatically
-- Super Admin: first account seeded via migration, has full system access
CREATE TABLE admins (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name            VARCHAR(255) NOT NULL,
    email           VARCHAR(255) UNIQUE NOT NULL,
    password_hash   VARCHAR(255) NOT NULL,
    company_name    VARCHAR(255) NOT NULL,
    plan_id         UUID NOT NULL REFERENCES plans(id),  -- Defaults to 'free' plan on registration
    role            VARCHAR(20) NOT NULL DEFAULT 'admin'
                    CHECK (role IN ('admin', 'superadmin')),
    email_verified  BOOLEAN DEFAULT FALSE,
    is_active       BOOLEAN DEFAULT TRUE,
    created_at      TIMESTAMPTZ DEFAULT NOW(),
    updated_at      TIMESTAMPTZ DEFAULT NOW()
);

-- Seed the first super admin (password hash generated at deploy time)
-- INSERT INTO admins (name, email, password_hash, company_name, plan_id, role, email_verified)
-- VALUES ('System Admin', 'admin@holeritesign.com.br', '<bcrypt_hash>', 'HoleriteSign',
--         (SELECT id FROM plans WHERE name = 'enterprise'), 'superadmin', TRUE);

-- Employees managed by admins
CREATE TABLE employees (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    admin_id        UUID NOT NULL REFERENCES admins(id),
    name            VARCHAR(255) NOT NULL,
    email           VARCHAR(255),
    whatsapp        VARCHAR(20),  -- E.164 format: +5511999999999
    
    -- PII for identity verification (SIG-11)
    -- Stored encrypted (AES-256) at application level via EF Core Value Converters.
    -- Never stored in plaintext. Decrypted only at verification time.
    cpf_encrypted   BYTEA,                  -- Full CPF encrypted (11 digits)
    cpf_last4       CHAR(4),                -- Last 4 digits of CPF (for display: "***.***.XXX-XX")
    birth_date_encrypted BYTEA,             -- Date of birth encrypted
    -- Note: at least one of cpf_encrypted or birth_date_encrypted is required
    -- when identity verification (SIG-11) is enabled for signing.
    
    is_active       BOOLEAN DEFAULT TRUE,
    created_at      TIMESTAMPTZ DEFAULT NOW(),
    updated_at      TIMESTAMPTZ DEFAULT NOW(),
    deleted_at      TIMESTAMPTZ,  -- Soft delete: employee is hidden from new periods
                                    -- but remains visible in historical months' data
    
    CONSTRAINT chk_contact CHECK (email IS NOT NULL OR whatsapp IS NOT NULL)
);

CREATE INDEX idx_employees_admin ON employees(admin_id);

-- Monthly periods for organizing documents
CREATE TABLE pay_periods (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    admin_id        UUID NOT NULL REFERENCES admins(id),
    year            INTEGER NOT NULL,
    month           INTEGER NOT NULL CHECK (month BETWEEN 1 AND 12),
    label           VARCHAR(50),  -- e.g., "Fevereiro 2026"
    created_at      TIMESTAMPTZ DEFAULT NOW(),
    
    UNIQUE(admin_id, year, month)
);

-- Documents (payslip PDFs)
CREATE TABLE documents (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_id         UUID NOT NULL REFERENCES employees(id),
    pay_period_id       UUID NOT NULL REFERENCES pay_periods(id),
    admin_id            UUID NOT NULL REFERENCES admins(id),
    
    -- Original file
    original_filename   VARCHAR(500) NOT NULL,
    original_file_key   VARCHAR(500) NOT NULL,  -- S3 key
    original_file_hash  VARCHAR(64) NOT NULL,   -- SHA-256 of original PDF
    file_size_bytes     BIGINT NOT NULL,
    
    -- Signed file (populated after signature)
    signed_file_key     VARCHAR(500),           -- S3 key of signed PDF
    signed_file_hash    VARCHAR(64),            -- SHA-256 of signed PDF
    
    -- Status tracking
    status              VARCHAR(20) DEFAULT 'uploaded' 
                        CHECK (status IN ('uploaded', 'sent', 'signed', 'expired')),
    -- Note (DATA-01): 'viewed' removed from status enum. Use viewed_at timestamp instead.
    
    -- Signing token (SEC-15: store hash only, never raw token)
    signing_token_hash  CHAR(64) UNIQUE,        -- SHA-256 hash of the raw token
    token_expires_at    TIMESTAMPTZ,
    token_used_at       TIMESTAMPTZ,
    
    -- Tracking timestamps (DATA-02)
    viewed_at           TIMESTAMPTZ,             -- When employee first opened the signing page
    last_notified_at    TIMESTAMPTZ,             -- When the last notification was sent
    
    created_at          TIMESTAMPTZ DEFAULT NOW(),
    updated_at          TIMESTAMPTZ DEFAULT NOW(),
    
    UNIQUE(employee_id, pay_period_id)  -- One document per employee per period
);

CREATE INDEX idx_documents_employee ON documents(employee_id);
CREATE INDEX idx_documents_period ON documents(pay_period_id);
CREATE INDEX idx_documents_token ON documents(signing_token_hash);
CREATE INDEX idx_documents_status ON documents(status);

-- Signatures
CREATE TABLE signatures (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id         UUID NOT NULL UNIQUE REFERENCES documents(id),
    employee_id         UUID NOT NULL REFERENCES employees(id),
    
    -- Selfie photo
    photo_file_key      VARCHAR(500) NOT NULL,  -- S3 key
    photo_hash          VARCHAR(64) NOT NULL,   -- SHA-256 of photo
    photo_mime_type     VARCHAR(50) NOT NULL,
    
    -- Signer identification metadata
    signer_ip           INET NOT NULL,
    signer_user_agent   TEXT NOT NULL,
    signer_geolocation  JSONB,  -- { lat, lng, accuracy } if available
    signer_device_info  JSONB,  -- Additional device fingerprint data
    
    -- Timestamp
    signed_at           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    
    -- Legal
    consent_given       BOOLEAN NOT NULL DEFAULT TRUE,
    consent_text        TEXT NOT NULL,  -- The exact text the user agreed to
    
    -- Verification (SIG-11 / SIG-12)
    verification_method VARCHAR(10) CHECK (verification_method IN ('otp', 'cpf', 'dob')),
    verification_hash   CHAR(64),       -- SHA-256 of the verification value submitted
    
    created_at          TIMESTAMPTZ DEFAULT NOW()
);

-- =====================================================
-- AUDIT LOG (Immutable, append-only)
-- =====================================================

CREATE TABLE audit_logs (
    id              BIGSERIAL PRIMARY KEY,
    
    -- Context
    admin_id        UUID REFERENCES admins(id),
    employee_id     UUID REFERENCES employees(id),
    document_id     UUID REFERENCES documents(id),
    
    -- Event
    event_type      VARCHAR(50) NOT NULL,
    -- Events: 'document.uploaded', 'document.replaced', 'token.generated',
    --         'token.expired', 'notification.sent.email', 'notification.sent.whatsapp',
    --         'notification.delivered', 'signing_page.accessed',
    --         'pdf.opened', 'pdf.viewed', 'pdf.view.completed', 'photo.captured',
    --         'sign.verify.started', 'sign.verify.passed', 'sign.verify.failed',
    --         'signature.submitted', 'signed_pdf.generated', 'document.downloaded',
    --         'super.impersonate.started', 'super.impersonate.ended'
    
    event_data      JSONB,          -- Additional event-specific data
    
    -- Actor info
    actor_type      VARCHAR(20) NOT NULL CHECK (actor_type IN ('admin', 'employee', 'system')),
    -- Note: superadmin actions use actor_type = 'admin' with role stored in event_data.actor_role
    actor_ip        INET,
    actor_user_agent TEXT,
    
    -- Hash chain (SEC-16: tamper-evident audit trail)
    -- Chain is scoped PER DOCUMENT (document_id). Each document has its own independent chain.
    -- This avoids global serialization bottlenecks while still detecting per-document tampering.
    -- For entries without a document_id (e.g., admin login), chain is scoped per admin_id.
    prev_hash       CHAR(64),                   -- SHA-256 hash of the previous audit entry in this chain (NULL for first entry)
    entry_hash      CHAR(64) NOT NULL,           -- SHA-256(id + event_type + event_data + created_at + prev_hash)
    chain_version   INT NOT NULL DEFAULT 1,      -- Algorithm version for future upgrades
    
    -- Immutable timestamp
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_audit_document ON audit_logs(document_id);
CREATE INDEX idx_audit_employee ON audit_logs(employee_id);
CREATE INDEX idx_audit_event ON audit_logs(event_type);
CREATE INDEX idx_audit_created ON audit_logs(created_at);

-- Prevent updates and deletes on audit_logs
-- Enforced via RESTRICTED DB USER: API connects with a user that has
-- INSERT + SELECT only on this table (no UPDATE, no DELETE).
-- Additionally, a DB trigger as a second safety layer:

CREATE OR REPLACE FUNCTION prevent_audit_modification()
RETURNS TRIGGER AS $$
BEGIN
    RAISE EXCEPTION 'audit_logs table is immutable: % operations are not allowed', TG_OP;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_audit_no_update
    BEFORE UPDATE ON audit_logs
    FOR EACH ROW EXECUTE FUNCTION prevent_audit_modification();

CREATE TRIGGER trg_audit_no_delete
    BEFORE DELETE ON audit_logs
    FOR EACH ROW EXECUTE FUNCTION prevent_audit_modification();

-- =====================================================
-- NOTIFICATIONS
-- =====================================================

CREATE TABLE notifications (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id     UUID NOT NULL REFERENCES documents(id),
    employee_id     UUID NOT NULL REFERENCES employees(id),
    
    channel         VARCHAR(20) NOT NULL CHECK (channel IN ('email', 'whatsapp')),
    status          VARCHAR(20) DEFAULT 'pending'
                    CHECK (status IN ('pending', 'sent', 'delivered', 'read', 'failed')),
    
    external_id     VARCHAR(255),  -- ID from SendGrid/WhatsApp API
    error_message   TEXT,
    
    sent_at         TIMESTAMPTZ,
    delivered_at    TIMESTAMPTZ,
    read_at         TIMESTAMPTZ,
    
    created_at      TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_notifications_document ON notifications(document_id);

-- =====================================================
-- SIGNING VERIFICATIONS (identity check before signing)
-- =====================================================

CREATE TABLE signing_verifications (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id     UUID NOT NULL REFERENCES documents(id),
    employee_id     UUID NOT NULL REFERENCES employees(id),
    
    -- Verification method and state
    method          VARCHAR(10) NOT NULL CHECK (method IN ('otp', 'cpf', 'dob')),
    verified        BOOLEAN NOT NULL DEFAULT FALSE,
    verified_at     TIMESTAMPTZ,
    
    -- OTP-specific fields (NULL for cpf/dob)
    otp_hash        CHAR(64),               -- SHA-256 of the OTP code sent
    otp_expires_at  TIMESTAMPTZ,            -- OTP valid for 5 minutes
    last_sent_at    TIMESTAMPTZ,            -- Cooldown: min 60s between resends
    attempt_count   INT NOT NULL DEFAULT 0, -- Max 3 attempts per 10 min window
    attempt_window_start TIMESTAMPTZ,       -- Start of current attempt window
    
    -- Session
    expires_at      TIMESTAMPTZ NOT NULL,   -- Verification session expires (same as token)
    
    created_at      TIMESTAMPTZ DEFAULT NOW(),
    updated_at      TIMESTAMPTZ DEFAULT NOW()
);

CREATE INDEX idx_signing_verifications_document ON signing_verifications(document_id);
CREATE UNIQUE INDEX idx_signing_verifications_active ON signing_verifications(document_id) WHERE verified = FALSE;
```

### 5.4 API Endpoints

#### Authentication
```
POST   /api/auth/register           Self-registration (name, email, company, password) → Free plan
POST   /api/auth/verify-email       Verify email with token
POST   /api/auth/login              Admin login
POST   /api/auth/logout             Admin logout
POST   /api/auth/refresh            Refresh access token
POST   /api/auth/forgot-password    Request password reset
POST   /api/auth/reset-password     Reset password with token
```

#### Employees
```
GET    /api/employees               List employees (paginated, searchable)
POST   /api/employees               Create employee
GET    /api/employees/{id}          Get employee details + full document history
PUT    /api/employees/{id}          Update employee
DELETE /api/employees/{id}          Soft-delete (deactivate) — preserves past months
POST   /api/employees/import        Bulk import from CSV
```

#### Pay Periods
```
GET    /api/pay-periods             List pay periods
POST   /api/pay-periods             Create pay period (auto-populates active employees)
GET    /api/pay-periods/{id}        Get period with document status summary
GET    /api/pay-periods/{id}/pending Get employees who have NOT signed yet
```

#### Documents
```
POST   /api/documents/upload        Upload payslip PDF for employee+period
GET    /api/documents/{id}          Get document details + audit trail
PUT    /api/documents/{id}/replace  Replace PDF (before signing only)
DELETE /api/documents/{id}          Delete document (before signing only)
GET    /api/documents/{id}/download Download original PDF
GET    /api/documents/{id}/signed   Download signed PDF
POST   /api/documents/{id}/send     Send notification to employee
POST   /api/documents/send-bulk     Send notifications for all pending in a period
```

#### Signing (Public — no auth, token-based)
```
GET    /api/sign/{token}            Validate token, return document metadata
GET    /api/sign/{token}/pdf        Redirect to presigned R2/S3 URL (saves API bandwidth; PDF served directly from CDN/storage)
POST   /api/sign/{token}/verify     Verify employee identity (CPF/DOB or OTP) — required before submit (SIG-11/12)
POST   /api/sign/{token}/submit     Submit signature (selfie photo + consent). Idempotent (SEC-19): re-submitting same token returns existing signature. Requires prior verification.
GET    /api/sign/{token}/download   Download signed PDF (after signature, low priority)
```

#### Dashboard
```
GET    /api/dashboard/overview      Summary stats + plan usage
GET    /api/dashboard/period/{id}   Detailed status for a pay period
GET    /api/dashboard/pending       Who hasn't signed yet (all periods or filtered)
GET    /api/audit-logs              Paginated audit log (filterable)
```

#### Backup & Export
```
GET    /api/export/period/{id}      Download all signed PDFs for a month as ZIP
GET    /api/export/all              Download ALL data as ZIP (full backup)
GET    /api/export/employees        Export employee list as CSV
GET    /api/export/audit-logs       Export audit logs as JSON/CSV
```

#### Plans (System operator only)
```
GET    /api/plans                   List available plans
PUT    /api/admins/{id}/plan        Change an admin's plan
GET    /api/admins/{id}/usage       Get current usage vs. plan limits
```

#### Super Admin (superadmin role only)
```
GET    /api/super/accounts          List all admin accounts + stats
GET    /api/super/accounts/{id}     View specific account details
PUT    /api/super/accounts/{id}     Update account (plan, active status)
DELETE /api/super/accounts/{id}     Deactivate account
POST   /api/super/accounts/{id}/impersonate  Get a temporary token to view an admin's panel (for debugging)
GET    /api/super/metrics           Global metrics (total accounts, documents, signatures)
GET    /api/super/audit-logs        Global audit logs (across all accounts)
POST   /api/super/plans             Create new plan tier
PUT    /api/super/plans/{id}        Edit plan limits
POST   /api/super/admins            Create another superadmin account
```

---

## 6. Detailed Module Design

### 6.1 Signing Token System

Each document gets a unique signing token when the admin sends a notification:

```
Token format: Base64URL(random 64 bytes) → 86 characters
URL: https://app.holeritesign.com.br/assinar/{token}

Properties:
  - Cryptographically random (crypto.randomBytes)
  - Single-use (invalidated after signature submission)
  - Time-limited (configurable, default 7 days)
  - Maps to exactly one document + employee
```

**Security measures:**
- Token is checked for expiration on every access
- Rate limited: max 10 requests per minute per IP to `/api/sign/*`
- After successful signature, token is permanently invalidated
- Tokens cannot be reused or regenerated without admin action

### 6.2 Selfie Capture & Photo Handling

```
Browser Camera Flow:
  1. Request camera permission (navigator.mediaDevices.getUserMedia)
  2. Show live camera feed (front-facing preferred)
  3. User taps "Tirar Foto" button
  4. Capture frame as JPEG (quality: 0.85, max resolution: 1920x1080)
  5. Show preview with "Refazer" / "Confirmar" options
  6. On confirm:
     a. Compute SHA-256 hash client-side
     b. Collect metadata (timestamp, screen resolution)
     c. Upload photo to server
     d. Server validates and stores in S3
     e. Server generates signed PDF
```

**Photo metadata collected:**
- Timestamp (client + server, both in UTC)
- SHA-256 hash of the image file
- Image dimensions and file size
- Device camera info (if available from MediaDevices API)

### 6.3 Signed PDF Generation

The signed PDF is generated server-side using **QuestPDF** (C#):

```
Signed PDF Structure:
┌──────────────────────────────────┐
│  [Original payslip pages 1..N]   │
│  + watermark: "Assinado          │
│    digitalmente via HoleriteSign"│
├──────────────────────────────────┤
│  SIGNATURE PAGE                  │
│                                  │
│  ┌─────────────┐                │
│  │  Selfie      │  Employee Name │
│  │  Photo       │  Date/Time     │
│  │             │  IP Address    │
│  └─────────────┘                │
│                                  │
│  "Eu, [Nome], confirmo o         │
│   recebimento e ciência do       │
│   holerite referente a [mês/ano]"│
│                                  │
│  ─────────────────────────────── │
│  AUDIT TRAIL                     │
│  ─────────────────────────────── │
│  Document hash (SHA-256):        │
│    abc123...                     │
│  Photo hash (SHA-256):           │
│    def456...                     │
│  Signed at: 2026-02-25 14:32 UTC│
│  IP: 189.xxx.xxx.xxx            │
│  User-Agent: Mozilla/5.0...     │
│  Geolocation: -23.55, -46.63    │
│  Token: abc***xyz (partial)      │
│                                  │
│  Verification: This document was │
│  signed electronically per Law   │
│  14.063/2020 (Art. 4°, §1°)     │
└──────────────────────────────────┘
```

### 6.4 Notification System

```
WhatsApp Flow (via Meta Cloud API or Z-API) — CANAL PRINCIPAL:
  1. Admin triggers send (individual or bulk)
  2. Job pushed to Hangfire background queue
  3. Worker picks up job
  4. Sends template message via WhatsApp API:
     "Olá {nome}! Seu holerite de {mês/ano} está disponível.
      Acesse o link para visualizar e assinar:
      {link}
      Este link é válido por {dias} dias."
  5. Track delivery status via webhook callbacks
  6. Log everything to audit_logs

Email Flow (via SendGrid/SES):
  1. Same trigger mechanism
  2. Send HTML email with:
     - Company logo
     - Clear call-to-action button
     - Link to signing page
     - Expiration notice
  3. Track open/click via SendGrid events
  4. Log everything to audit_logs
```

---

## 7. Legal Compliance Details

### 7.1 Law 14.063/2020 — Electronic Signatures

This law establishes three levels of electronic signatures in Brazil:

| Level | Description | Our Approach |
|-------|-------------|--------------|
| **Simples** | Basic e-signature (e.g., checkbox, typed name) | ❌ Not sufficient |
| **Avançada** | Advanced e-signature: must identify the signer, be uniquely linked to them, use data under their control, detect modifications | ✅ **Our target level** |
| **Qualificada** | Uses ICP-Brasil digital certificate | ❌ Not required for payslips |

**How HoleriteSign qualifies as "Avançada":**

1. **Unique identification of the signer** → Selfie photo captured at time of signing
2. **Linked to the signer uniquely** → Token is mapped to a specific employee + document
3. **Data under signer's exclusive control** → Only the employee receives the unique link; selfie is taken from their device
4. **Detect subsequent modifications** → SHA-256 hashes of both original PDF and selfie photo are embedded; any tampering is detectable
5. **Audit trail** → Complete log of every interaction with timestamps, IP, device info

### 7.2 MP 2.200-2/2001

Article 10, §2° allows electronic signatures not based on ICP-Brasil to be valid when:
- The parties agree to accept them
- The signature method can be verified

**Implementation:** The employee's act of taking a selfie and submitting constitutes acceptance. The consent text is recorded and embedded in the signed PDF.

### 7.3 LGPD (Lei 13.709/2018) Compliance

| Requirement | Implementation |
|-------------|---------------|
| **Legal basis** | Legitimate interest (Art. 7°, IX) + Consent (Art. 7°, I) for photo capture |
| **Data minimization** | Collect only: name, email, WhatsApp, CPF (encrypted), birth date (encrypted), selfie, device metadata |
| **Consent** | Explicit consent screen before selfie capture; consent text recorded and embedded in signed PDF |
| **Transparency** | Privacy policy link on signing page explaining data usage |
| **Right of access** | Admin panel allows export of all employee data |
| **Right of deletion** | Implement data deletion workflow (with legal retention exceptions) |
| **Data retention** | Documents retained for 5 years (labor law requirement), then auto-purged |
| **Encryption** | AES-256 at rest (including CPF and birth date via EF Core Value Converters), TLS 1.2+ in transit |
| **Access control** | Role-based access; employees can only see their own documents |
| **Incident response** | Logging + alerting for unauthorized access attempts |
| **DPO** | Contact information displayed in privacy policy |

#### 7.3.1 LGPD Roles (MVP)

| LGPD Role | Who | Responsibility |
|-----------|-----|----------------|
| **Controlador** (Controller) | The admin's company (client) | Determines the purpose and means of processing employee PII. The company decides to use HoleriteSign to collect payslip signatures. |
| **Operador** (Processor) | HoleriteSign (the SaaS platform / you as developer) | Processes PII on behalf of the controller. Stores encrypted data, generates signed PDFs, sends notifications. Must follow controller's instructions and LGPD obligations. |
| **Titular** (Data Subject) | The employee (signer) | The person whose data is processed. Has rights to access, correction, deletion (with retention exceptions). |
| **Encarregado (DPO)** | Defined per controller (client company) | Contact person for LGPD matters. For HoleriteSign itself, display a DPO contact email in the privacy policy (e.g., `privacidade@holeritesign.com.br`). |

> **Nota prática (MVP):** No MVP, o texto de consentimento e a política de privacidade devem estar prontos antes do primeiro uso com funcionários reais. O texto de consentimento aparece na tela de assinatura antes da selfie. A política de privacidade é acessível via link no rodapé da página de assinatura e no painel admin.

---

## 8. UI/UX Design Specifications

### 8.1 Admin Panel

**Color Palette:**
- Primary: `#2563EB` (Blue 600) — trust, professionalism
- Secondary: `#10B981` (Emerald 500) — success, completed actions  
- Warning: `#F59E0B` (Amber 500)
- Error: `#EF4444` (Red 500)
- Background: `#F8FAFC` (Slate 50)
- Card: `#FFFFFF`

**Responsividade:** O painel admin deve ser **responsivo** — funcionar bem em desktop (tela principal de trabalho) e em tablets/celulares (para consultas rápidas). Layout com sidebar colapsável em telas menores.

**Pages:**

1. **Login Page** — Clean centered form, company logo, email + password
2. **Dashboard** — Monthly overview cards, signature progress bars, **"Quem falta assinar"** em destaque, usage vs. plan limit
3. **Employees** — Searchable card grid/list view, each card shows: name, contact, last signature status. Employees persist across months.
4. **Employee Detail** — Profile info + full document history by month + audit timeline (history of all months, even after employee deactivation)
5. **Pay Period View** — Month selector → list of all employees with document/signature status + batch actions
6. **Upload** — Drag-and-drop zone, month selector, employee assignment
7. **Settings** — Company info, notification templates, token expiration config
8. **Backup/Export** — One-click bulk download of all data

### 8.2 Signing Page (Employee-Facing)

**Design Principles:**
- **Mobile-first** (90%+ will access via phone from WhatsApp link — é o canal principal)
- Maximum simplicity — one scroll, minimal text
- Large buttons, large touch targets (min 48px), clear calls-to-action
- Portuguese language only
- No navigation, no login, no distractions
- Optimized for camera usage on mobile devices (desktops rarely have cameras)

**Layout:**

```
┌────────────────────────────┐
│  [Company Logo]            │
│  "Seu holerite de          │
│   Fevereiro 2026"          │
├────────────────────────────┤
│                            │
│  ┌──────────────────────┐  │
│  │                      │  │
│  │   PDF VIEWER         │  │
│  │   (scrollable)       │  │
│  │                      │  │
│  └──────────────────────┘  │
│                            │
├────────────────────────────┤
│  "Para confirmar o         │
│   recebimento, tire uma    │
│   foto sua (selfie)"      │
│                            │
│  ┌──────────────────────┐  │
│  │  📷 Camera Preview   │  │
│  │  or Captured Photo    │  │
│  └──────────────────────┘  │
│                            │
│  [  Tirar Foto  ]  ← btn  │
│                            │
│  ☑ Li e concordo com os   │
│    termos de assinatura    │
│    eletrônica (link)       │
│                            │
│  [ Confirmar Assinatura ]  │
│    ← Large green button    │
│                            │
│  (After signing:)          │
│  [ Baixar meu holerite ]   │
│    ← Optional download btn │
│    (low priority feature)  │
├────────────────────────────┤
│  Powered by HoleriteSign   │
│  Política de Privacidade   │
└────────────────────────────┘
```

---

## 9. Infrastructure & Deployment

### 9.1 Environment Architecture

```
┌─────────────────────────────────────────┐
│             PRODUCTION (Bootstrapped)   │
│                                         │
│  Vercel (Free) ── React SPA (Frontend)  │
│     │                                   │
│     └── Railway/Render ($5) ── .NET API │
│              │                          │
│              ├── Neon.tech (Free PGSQL) │
│              ├── Cloudflare R2 (Free S3)│
│              ├── Upstash (Free Redis)   │
│              ├── SendGrid (Free Email)  │
│              └── WhatsApp Cloud API     │
│                                         │
│  Cloudflare ── DNS + CDN + DDoS         │
└──────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│             DEVELOPMENT                  │
│                                          │
│  localhost:5173 ── Vite dev server       │
│  localhost:5000 ── ASP.NET Core API      │
│  Docker Compose:                         │
│     ├── PostgreSQL 16                    │
│     ├── Redis 7                          │
│     └── MinIO (S3-compatible)            │
└──────────────────────────────────────────┘
```

### 9.2 Project Structure

```
holerite-sign/
├── frontend/                       # React SPA (Vite + TypeScript)
│   ├── src/
│   │   ├── pages/                  # Page components
│   │   │   ├── auth/
│   │   │   │   └── LoginPage.tsx
│   │   │   ├── admin/              # Admin routes (authenticated, responsive)
│   │   │   │   ├── DashboardPage.tsx
│   │   │   │   ├── EmployeesPage.tsx
│   │   │   │   ├── EmployeeDetailPage.tsx
│   │   │   │   ├── PayPeriodPage.tsx
│   │   │   │   ├── UploadPage.tsx
│   │   │   │   ├── BackupPage.tsx
│   │   │   │   └── SettingsPage.tsx
│   │   │   └── signing/            # Public signing routes (mobile-first)
│   │   │       ├── SigningPage.tsx
│   │   │       └── SigningSuccessPage.tsx
│   │   ├── components/
│   │   │   ├── ui/                 # shadcn/ui components
│   │   │   ├── admin/              # Admin-specific components
│   │   │   │   ├── Sidebar.tsx
│   │   │   │   ├── PendingSignaturesList.tsx
│   │   │   │   └── UsageMeter.tsx
│   │   │   ├── signing/            # Signing page components
│   │   │   │   ├── PdfViewer.tsx
│   │   │   │   ├── CameraCapture.tsx
│   │   │   │   └── SignatureForm.tsx
│   │   │   └── shared/             # Shared components
│   │   ├── hooks/                  # Custom React hooks
│   │   ├── services/               # API client (axios/fetch wrappers)
│   │   │   └── api.ts
│   │   ├── stores/                 # Zustand stores
│   │   ├── types/                  # TypeScript interfaces
│   │   ├── utils/
│   │   ├── App.tsx
│   │   ├── router.tsx              # React Router config
│   │   └── main.tsx
│   ├── public/
│   ├── index.html
│   ├── vite.config.ts
│   ├── tailwind.config.ts
│   ├── tsconfig.json
│   └── package.json
│
├── backend/                        # ASP.NET Core 8 Web API (C#)
│   ├── HoleriteSign.Api/           # Main API project
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs
│   │   │   ├── EmployeesController.cs
│   │   │   ├── DocumentsController.cs
│   │   │   ├── SigningController.cs
│   │   │   ├── DashboardController.cs
│   │   │   ├── ExportController.cs
│   │   │   └── PlansController.cs
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── HoleriteSign.Api.csproj
│   │
│   ├── HoleriteSign.Core/          # Domain models + interfaces
│   │   ├── Entities/
│   │   │   ├── Admin.cs
│   │   │   ├── Employee.cs
│   │   │   ├── Document.cs
│   │   │   ├── Signature.cs
│   │   │   ├── PayPeriod.cs
│   │   │   ├── Plan.cs
│   │   │   ├── AuditLog.cs
│   │   │   └── Notification.cs
│   │   ├── Interfaces/
│   │   └── HoleriteSign.Core.csproj
│   │
│   ├── HoleriteSign.Infrastructure/ # EF Core, S3, Email, WhatsApp
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations/      # EF Core entity configs
│   │   │   └── Migrations/
│   │   ├── Services/
│   │   │   ├── AuthService.cs
│   │   │   ├── EmployeeService.cs
│   │   │   ├── DocumentService.cs
│   │   │   ├── SignatureService.cs
│   │   │   ├── PdfService.cs        # QuestPDF
│   │   │   ├── StorageService.cs    # S3/MinIO
│   │   │   ├── WhatsAppService.cs
│   │   │   ├── EmailService.cs
│   │   │   ├── ExportService.cs     # Bulk ZIP download
│   │   │   └── AuditService.cs
│   │   ├── Jobs/                    # Hangfire background jobs
│   │   │   ├── NotificationJob.cs
│   │   │   └── PdfGenerationJob.cs
│   │   └── HoleriteSign.Infrastructure.csproj
│   │
│   ├── HoleriteSign.Tests/          # Unit + Integration tests
│   │   └── HoleriteSign.Tests.csproj
│   │
│   └── HoleriteSign.sln             # Solution file
│
├── docker/
│   ├── docker-compose.yml           # Local dev: PostgreSQL + Redis + MinIO
│   ├── Dockerfile.api
│   └── Dockerfile.frontend
│
├── docs/
│   ├── DESIGN_PLAN.md               # This document
│   ├── API.md                       # API documentation (Swagger auto-generated)
│   └── LEGAL.md                     # Legal compliance documentation
│
└── README.md
```

---

## 10. Development Phases & Milestones

> **Estratégia incremental:** O desenvolvimento será feito por partes, validando cada fase antes de avançar. Isso reduz o risco de quebrar tudo de uma vez.

### 10.0 MVP Scope Definition (Pre-Flight Check)

> **Corte do MVP:** O MVP é o mínimo necessário para validar o produto com o primeiro cliente (40 funcionários). Tudo marcado como "POST-MVP" será implementado após a validação com o cliente real.

| Feature | MVP? | Notes |
|---------|------|-------|
| Admin self-registration + Free plan | ✅ MVP | AUTH-01–10 |
| Employee CRUD + CSV import | ✅ MVP | EMP-01–07 |
| Document upload + pay periods | ✅ MVP | DOC-01–06 |
| WhatsApp link notification | ✅ MVP | NOT-01–04 |
| Email fallback notification | ✅ MVP | NOT-02 |
| **Identity verification (CPF/DOB)** | ✅ MVP | **SIG-11** (Must, zero-cost) |
| Selfie capture + submission | ✅ MVP | SIG-01–08 |
| Signed PDF generation (QuestPDF) | ✅ MVP | PDF-01–04 |
| Basic dashboard + "quem falta assinar" | ✅ MVP | DASH-01–07 |
| Audit trail (append-only) | ✅ MVP | AUD-01–05 |
| Export ZIP (signed PDFs per month) | ✅ MVP | BAK-01 |
| Plan enforcement (Free: 10 docs) | ✅ MVP | PLAN-01–08 |
| Consent text + privacy policy | ✅ MVP | LGPD compliance minimum |
| Dedicated error pages | ✅ MVP | SIG-16 |
| Confirmation checkbox + min time | ✅ MVP | SIG-13 |
| Camera fallback (gallery) | ✅ MVP | SIG-15 |
| OTP verification (WhatsApp) | ❌ POST-MVP | SIG-12 (Should) — adds cost + complexity |
| Scroll tracking | ❌ POST-MVP | SIG-14 (Should) — nice audit evidence |
| Photo retry + progress bar | ❌ POST-MVP | SIG-17 (Should) |
| Notification templates | ❌ POST-MVP | NOT-05 (Should) |
| Delivery status tracking | ❌ POST-MVP | NOT-06 (Should) |
| Super admin panel | ❌ POST-MVP | Only seeded account needed for MVP |
| Batch upload (filename pattern) | ❌ POST-MVP | DOC-03 (Should) |
| Scheduled automatic backup | ❌ POST-MVP | BAK-05 (Low) |
| Token expiration job (DOC-07) | ❌ POST-MVP | Should — manual check is fine for 40 employees |

### Phase 1 — Foundation & Scaffolding (Week 1)
- [x] Design plan document (this document)
- [ ] Project scaffolding: React+Vite frontend + ASP.NET Core backend
- [ ] Docker Compose for local development (PostgreSQL + Redis + MinIO)
- [ ] Database schema + EF Core migrations (including Plans table)
- [ ] Basic project structure (Clean Architecture)
- [ ] **Validação:** API roda, banco conecta, frontend carrega ✓

### Phase 2 — Auth & Admin Layout (Week 2)
- [ ] Self-registration page (name, email, company, password) → auto Free plan
- [ ] Email verification flow
- [ ] Admin authentication (login, logout, JWT)
- [ ] Protected routes in React (React Router guards)
- [ ] Basic admin layout: sidebar, header, responsive shell
- [ ] Login page (email + password)
- [ ] **Validação:** Admin cria conta sozinho, verifica email, faz login ✓

### Phase 3 — Employee CRUD (Week 3)
- [ ] Employee CRUD API (create, read, update, soft-delete)
- [ ] **CPF and birth date fields** — encrypted storage (AES-256 via EF Core Value Converters)
- [ ] CPF last-4 display in admin panel (masked: `***.***.XXX-XX`)
- [ ] Employee persistence across months (EMP-06)
- [ ] Historical integrity on deactivation (EMP-07)
- [ ] Employee listing page with search + cards
- [ ] Employee detail page
- [ ] **Validação:** Admin cadastra funcionários com CPF/data de nascimento, dados criptografados no banco ✓

### Phase 4 — Pay Periods & Document Upload (Week 4)
- [ ] Pay period management (create, list)
- [ ] Auto-populate active employees into new periods
- [ ] Document upload (single PDF per employee per month)
- [ ] S3/MinIO integration for file storage
- [ ] PDF preview in admin panel
- [ ] Plan usage tracking (documents used vs. limit)
- [ ] **Validação:** Admin cria período, faz upload de holerites ✓

### Phase 5 — Signing Flow (Weeks 5–6)
- [ ] Token generation system (tokenHash stored, raw sent via notification)
- [ ] Public signing page (no auth, mobile-first)
- [ ] **Identity verification step (SIG-11)** — CPF or DOB check against employee record
- [ ] `signing_verifications` table integration
- [ ] PDF viewer (employee-facing, react-pdf)
- [ ] Confirmation checkbox + minimum viewing time (SIG-13)
- [ ] Camera capture component (selfie, front-facing)
- [ ] Camera fallback: gallery upload (SIG-15)
- [ ] Signature submission endpoint (idempotent, SEC-19)
- [ ] Signed PDF generation with QuestPDF (photo + audit trail embedded)
- [ ] Consent flow and text (LGPD — explicit consent screen before selfie)
- [ ] Dedicated error pages: expired, already signed, not found (SIG-16)
- [ ] **Validação:** Funcionário recebe link, verifica CPF/DOB, vê PDF, tira selfie, assina ✓

### Phase 6 — Notifications (Week 7)
- [ ] WhatsApp notification service (primary channel)
- [ ] Email notification service (fallback)
- [ ] Hangfire background job queue
- [ ] Bulk send functionality
- [ ] Delivery status tracking
- [ ] **Validação:** Admin envia notificações em lote via WhatsApp ✓

### Phase 7 — Dashboard & Backup (Week 8)
- [ ] Admin dashboard with statistics + "quem falta assinar" section
- [ ] Status tracking (pending → sent → signed)
- [ ] Usage meter (plan limit visualization)
- [ ] Audit log viewer in admin
- [ ] Bulk export: download all signed PDFs as ZIP
- [ ] Employee list export (CSV)
- [ ] **Validação:** Dashboard funcional, export ZIP funciona ✓

### Phase 8 — Security & Compliance (Week 9)
- [ ] Security hardening (rate limiting, CORS, input validation)
- [ ] LGPD consent flows (explicit consent screen on signing page)
- [ ] Privacy policy page (accessible from signing page and admin panel)
- [ ] Define LGPD roles in documentation (see §7.3)
- [ ] Data retention automation
- [ ] Penetration testing checklist
- [ ] Legal review

### Phase 9 — Testing & Deployment (Week 10)
- [x] Unit tests (services, C# xUnit) — 30 AuthService + EmployeeService + SigningService tests
- [x] Integration tests (API endpoints) — 6 API tests via WebApplicationFactory
- [x] Validator tests — 14 FluentValidation tests
- [ ] E2E tests (admin flow, signing flow)
- [x] Production deployment setup — Dockerfile (multi-stage), docker-compose.prod.yml, .env.prod.example
- [x] Monitoring and alerting — Health checks in Dockerfile + docker-compose
- [x] Documentation finalization (Swagger auto-docs) — Swashbuckle with JWT bearer auth

---

## 11. Environment Variables

### Backend (appsettings.json / Environment Variables)
```json
{
  "App": {
    "FrontendUrl": "https://app.holeritesign.com.br",
    "ApiUrl": "https://api.holeritesign.com.br"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=host;Port=5432;Database=holeritesign;Username=user;Password=pass",
    "Redis": "host:6379,password=pass"
  },
  "Jwt": {
    "Secret": "<random-64-bytes-hex>",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7,
    "Issuer": "HoleriteSign",
    "Audience": "HoleriteSign"
  },
  "Storage": {
    "Bucket": "holeritesign-documents",
    "Region": "sa-east-1",
    "AccessKey": "<key>",
    "SecretKey": "<secret>",
    "Endpoint": "<optional-for-minio>"
  },
  "Email": {
    "SendGridApiKey": "<key>",
    "From": "noreply@holeritesign.com.br",
    "FromName": "HoleriteSign"
  },
  "WhatsApp": {
    "ApiUrl": "https://graph.facebook.com/v18.0",
    "AccessToken": "<token>",
    "PhoneNumberId": "<phone-number-id>",
    "TemplateName": "holerite_notification"
  },
  "Signing": {
    "TokenExpiryDays": 7,
    "TokenLength": 64
  },
  "Sentry": {
    "Dsn": "<dsn>"
  }
}
```

### Frontend (.env)
```env
VITE_API_URL=https://api.holeritesign.com.br
VITE_APP_NAME=HoleriteSign
```

---

## 12. Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| WhatsApp API approval delay | Medium | High | Start with email-only; apply for WhatsApp Business API early |
| Employee refuses selfie / camera permission | Medium | Medium | Provide clear instructions; fallback to gallery upload (SIG-14) |
| PDF too large for inline viewing | Low | Medium | Set 10MB limit; compress server-side |
| Legal challenge on signature validity | Low | High | Comprehensive audit trail; embedded hashes; hash chain (SEC-16); legal counsel review |
| Employee shares signing link | Low | Medium | Token is single-use + IP is logged; CPF/DOB verification (SIG-11) adds identity check; link expires after signing |
| Data breach | Low | Critical | Encryption at rest + transit; minimal data collection; token hashing (SEC-15); access logs; incident response plan |
| WhatsApp message delivery failures | Medium | Medium | Fallback to email; retry queue; delivery status monitoring |
| Audit log concurrency under hash chain | Low | Low | Chain scoped per document_id — no global lock contention; advisory lock per document (see §12.1 IMP-02) |
| Double-submit race condition | Medium | Medium | Redis distributed lock keyed on token hash + idempotent endpoint (SEC-19, see §12.1 IMP-03) |

### 12.1 Implementation Warnings ⚠️

Practical considerations that affect cost, concurrency, and correctness during implementation.

| ID | Topic | Details |
|----|-------|--------|
| IMP-01 | **WhatsApp OTP is optional (Should)** | SIG-11 (CPF/DOB) is the default Must verification — zero cost per verification. OTP via WhatsApp (SIG-12) is a Should-tier upgrade. If enabled, it adds ~$0.05/msg per OTP. At 40 employees this is ~$2/mo extra, but at scale with retries it can add up. Rate limit OTP sends (3 attempts/10min, 60s cooldown, 5min expiry) to prevent abuse and cost spikes. |
| IMP-02 | **Audit log concurrency (hash chain)** | Hash chain is scoped **per `document_id`** (not globally), so concurrent signatures on different documents never contend. Within a single document chain, use a short **EF Core advisory lock** (keyed on `document_id`) around audit inserts. At payslip-signature volumes this is negligible. |
| IMP-03 | **Idempotency & race conditions (SEC-19)** | Two rapid clicks on "Confirmar Assinatura" could trigger parallel `POST /api/sign/{token}/submit` calls. Use a **Redis distributed lock** (key = `sign:{token_hash}`, TTL = 30s) acquired at the start of the request. If the lock is already held, return the existing signature. This guarantees exactly-once PDF generation. |
| IMP-04 | **PDF delivery via presigned redirect** | `GET /api/sign/{token}/pdf` should return a **302 redirect to a presigned R2/S3 URL** (TTL = 5–15 min, `Content-Disposition: inline`) instead of streaming through the API. This offloads bandwidth to Cloudflare R2 (free egress), reduces Railway/Render load, and allows CDN caching via `Cache-Control` headers. |
| IMP-05 | **Free tier egress/storage limits** | The first bottleneck on free tiers will be storage (Neon 0.5GB, R2 10GB) and Redis commands (Upstash 10k/day). Mitigate: use CDN cache headers for PDF viewer, presigned URLs with TTL, and avoid unnecessary Redis round-trips (e.g., cache verification state in the `signing_verifications` DB table, not Redis). |

---

## 13. Cost Estimation (MVP Bootstrapped — 1 Client, 40 Employees)

> **Estratégia de Custos:** A arquitetura foi desenhada para ser escalável (Enterprise-grade), mas a infraestrutura inicial utilizará *Free Tiers* (camadas gratuitas) de serviços modernos em nuvem. Isso permite validar o produto com o primeiro cliente (40 funcionários) com risco financeiro quase zero. Os custos de infraestrutura só aumentarão quando a receita dos clientes pagantes também aumentar.

| Service | Plan / Usage | Estimated Cost (Monthly) |
|---------|-------------|---------------------------|
| **Vercel** (Frontend SPA) | Hobby Tier (Free) | $0.00 |
| **Railway / Render** (API) | Hobby/Basic Tier (evita que a API "durma") | ~$5.00 |
| **Neon.tech** (PostgreSQL) | Free Tier (0.5 GB, suficiente para anos de logs) | $0.00 |
| **Cloudflare R2 / AWS S3** | Free Tier (10 GB/mês grátis. Uso est. < 100MB) | $0.00 |
| **Upstash** (Redis Cache) | Free Tier (10.000 comandos/dia) | $0.00 |
| **SendGrid** (Email) | Free Tier (100 emails/dia) | $0.00 |
| **WhatsApp Cloud API** (Meta) | ~80 envios (40 links + 40 OTPs) a ~$0.05/msg | ~$4.00 |
| **Cloudflare** (DNS/DDoS) | Free Tier | $0.00 |
| **Domain** (.com.br) | Amortizado (R$ 40/ano) | ~$0.70 |
| **Total Estimado** | **Custo operacional base para validar o SaaS** | **~$9.70 / mês (R$ 50)** |

*Nota: Quando a plataforma adquirir mais clientes e o limite dos planos gratuitos for atingido, o custo de upgrade (ex: Vercel Pro por $20, banco maior) será financiado pela própria receita recorrente das assinaturas (MRR).*

---

## 14. Open Questions & Decisions Needed

| # | Question | Options | Status |
|---|----------|---------|--------|
| 1 | Domain name? | holeritesign.com.br / assinaponto.com.br / other | ⏳ Pending |
| 2 | WhatsApp API provider? | Meta Cloud API (official). At 40 employees, cost is ~$4/mo. | ✅ Decided |
| 3 | Multi-tenant (multiple companies) from day one? | Yes (each admin = one company) — schema already supports this | ✅ Yes |
| 4 | Email provider? | SendGrid (Free tier handles 100 emails/day perfectly for MVP). | ✅ Decided |
| 5 | Hosting/Infra providers? | Vercel (Front), Neon (DB), Cloudflare R2 (Storage), Render/Railway (API). | ✅ Decided |
| 6 | Selfie validation (liveness detection)? | Not in MVP / Add later | ⏳ Pending |
| 7 | Plan pricing tiers? | Free (10 docs), Basic (50), Pro (200), Enterprise (unlimited) — defined in DB | ✅ Defined |
| 8 | Account creation model? | **Self-registration** — admin creates own account, auto-assigned Free plan | ✅ Decided |
| 9 | Frontend framework? | React + TypeScript + Vite + Tailwind | ✅ Decided |
| 10 | Backend framework? | ASP.NET Core 8 (C#) | ✅ Decided |
| 11 | Database? | PostgreSQL 16 | ✅ Decided |

---

## 15. Glossary

| Term | Definition |
|------|-----------|
| **Holerite** | Brazilian term for payslip/pay stub |
| **CLT** | Consolidação das Leis do Trabalho — Brazilian labor law framework for formal employment |
| **LGPD** | Lei Geral de Proteção de Dados — Brazil's General Data Protection Law (similar to GDPR) |
| **ICP-Brasil** | Infraestrutura de Chaves Públicas Brasileira — Brazil's official PKI |
| **Assinatura Eletrônica Avançada** | Advanced electronic signature as defined by Law 14.063/2020 |
| **PontoTel** | Time tracking SaaS product used by the client's employees |

---

*End of Design Plan — Version 2.4*
