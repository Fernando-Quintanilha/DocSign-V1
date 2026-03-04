# PROJETO DE BANCO DE DADOS — HoleriteSign

**Disciplina:** Banco de Dados  
**Professor:** Wanderson P. Medeiros  
**Data de Entrega:** 01/04/2026  

**Integrantes:**
1. Fernando Quintanilha  
2. Ian Gabriel 
3. Ana Isabela
4. Hadonay

---

## 1.1 — Introdução

### Descrição do Problema

Empresas de pequeno e médio porte enfrentam um desafio significativo na gestão e distribuição de holerites (contracheques) para seus funcionários. O processo tradicional envolve a impressão física de documentos, entrega manual — muitas vezes dependendo da presença do funcionário no local de trabalho — e a coleta de assinaturas em papel como comprovante de recebimento. Esse fluxo apresenta diversos problemas:

- **Custo elevado:** impressão, papel, tinta e armazenamento físico geram despesas recorrentes.
- **Ineficiência operacional:** o departamento de RH/financeiro gasta horas distribuindo documentos e cobrando assinaturas pendentes.
- **Falta de rastreabilidade:** não há controle preciso de quem recebeu, visualizou ou assinou o documento.
- **Risco de extravio:** documentos físicos podem ser perdidos, danificados ou acessados por pessoas não autorizadas.
- **Não conformidade com a LGPD:** dados sensíveis como CPF e informações salariais circulam sem proteção adequada.

### Contextualização da Solução

O **HoleriteSign** é uma plataforma SaaS (Software as a Service) web que digitaliza todo o ciclo de vida do holerite: desde o upload do documento PDF pelo administrador da empresa, passando pela notificação ao funcionário via WhatsApp, até a assinatura digital com verificação de identidade (CPF e data de nascimento) e captura de selfie como prova de aceite.

O **banco de dados** é o componente central desta solução. Ele é responsável por:

- **Armazenar e gerenciar dados de múltiplas empresas** (multi-tenancy) com isolamento total entre clientes.
- **Garantir a integridade e rastreabilidade** de todo o processo através de uma cadeia de auditoria com hash encadeado (tamper-evident).
- **Proteger dados pessoais sensíveis** (CPF, data de nascimento) através de criptografia AES-256 no nível da aplicação, armazenados como bytes criptografados no banco.
- **Controlar o ciclo de vida dos documentos** — do upload à assinatura — com estados bem definidos e transições registradas.
- **Gerenciar planos de assinatura** com limites de documentos e funcionários por empresa.

O banco de dados PostgreSQL 16 foi implementado em nuvem, rodando em um container Docker em um VPS (Virtual Private Server), garantindo alta disponibilidade, backups automatizados e escalabilidade.

### Tabelas do Banco de Dados e seus Objetivos

O sistema é composto por **9 tabelas** principais:

| # | Tabela | Objetivo |
|---|--------|----------|
| 1 | `plans` | Gerenciar planos de assinatura com limites de uso |
| 2 | `admins` | Armazenar dados das empresas/administradores cadastrados |
| 3 | `employees` | Cadastrar funcionários com dados pessoais criptografados |
| 4 | `pay_periods` | Organizar períodos de pagamento (mês/ano) |
| 5 | `documents` | Controlar documentos PDF com estados e tokens de assinatura |
| 6 | `signatures` | Registrar assinaturas digitais com metadados forenses |
| 7 | `signing_verifications` | Gerenciar verificação de identidade (CPF/data de nascimento) |
| 8 | `notifications` | Rastrear notificações enviadas via WhatsApp |
| 9 | `audit_logs` | Manter trilha de auditoria imutável com hash encadeado |

---

## 1.2 — Objetivos

### Objetivo Geral

Projetar e implementar um banco de dados relacional em nuvem utilizando PostgreSQL 16, capaz de suportar a gestão completa do ciclo de vida de holerites digitais para múltiplas empresas simultaneamente, garantindo segurança, integridade e conformidade com a LGPD.

### Objetivos Específicos

1. **Modelar um banco de dados normalizado (3FN)** com 9 tabelas inter-relacionadas, utilizando chaves primárias UUID, chaves estrangeiras, constraints de check e índices otimizados.

2. **Implementar multi-tenancy** via filtros globais de consulta (Global Query Filters) no ORM, garantindo isolamento total dos dados entre diferentes empresas clientes.

3. **Aplicar criptografia AES-256** para proteção de dados sensíveis (CPF e data de nascimento) armazenados como bytea, em conformidade com a LGPD.

4. **Desenvolver uma trilha de auditoria imutável** utilizando hash encadeado SHA-256, garantindo rastreabilidade e não-repúdio de todas as operações realizadas no sistema.

5. **Implementar operações CRUD completas** (Create, Read, Update, Delete) com soft delete para exclusão lógica de registros, preservando integridade referencial.

6. **Hospedar o banco de dados em nuvem** via Docker em VPS, demonstrando escalabilidade e acessibilidade remota.

7. **Gerenciar controle de acesso e planos** com tabela de planos que define limites de documentos e funcionários por empresa.

---

## 1.3 — Justificativa

### Relevância para o Comerciante/Indústria

A solução HoleriteSign atende uma necessidade real e recorrente de empresas brasileiras. Segundo pesquisas do mercado, mais de 70% das pequenas e médias empresas brasileiras ainda utilizam processos manuais para distribuição de holerites. Isso resulta em:

- Perda de produtividade de até 8 horas/mês por funcionário de RH
- Custos com impressão estimados em R$2,00–5,00 por holerite distribuído
- Riscos jurídicos por falta de comprovante de entrega e aceite do funcionário
- Exposição de dados sensíveis sem proteção adequada

O banco de dados é o alicerce que permite resolver esses problemas de forma escalável, segura e automatizada. Um único banco de dados PostgreSQL é capaz de atender centenas de empresas simultaneamente, graças ao modelo multi-tenant implementado.

### Justificativa da Escolha Tecnológica

**PostgreSQL 16** foi o SGBD escolhido pelas seguintes razões:

| Critério | Justificativa |
|----------|---------------|
| **Conformidade ACID** | Garante atomicidade, consistência, isolamento e durabilidade das transações — crítico para operações financeiras |
| **Tipo `bytea`** | Suporte nativo a armazenamento de dados binários, usado para campos criptografados (CPF, data de nascimento) |
| **Tipo `jsonb`** | Armazenamento eficiente de dados semi-estruturados (geolocalização, informações de dispositivo, dados de eventos de auditoria) |
| **Índices parciais** | Permite criar índices condicionais (ex: verificações ativas `WHERE verified = false`), otimizando consultas |
| **Geração de UUID** | Função nativa `gen_random_uuid()` para chaves primárias distribuídas |
| **Check constraints** | Validação declarativa no nível do banco (status válidos, meses entre 1-12, roles permitidas) |
| **Identity columns** | Geração segura de IDs sequenciais (audit_logs) com `GENERATED ALWAYS AS IDENTITY` |
| **Ecossistema** | Integração madura com Entity Framework Core 8 via provedor Npgsql |
| **Licença** | Open-source (PostgreSQL License), sem custos de licenciamento |
| **Comunidade** | Um dos SGBDs mais utilizados globalmente, com documentação extensa e suporte ativo |

---

## 1.4 — Metodologia

### Etapas de Desenvolvimento

O projeto seguiu a metodologia ágil com as seguintes etapas:

**Etapa 1 — Levantamento de Requisitos (Semana 1)**
- Identificação das necessidades do comerciante/empresa
- Mapeamento de processos existentes (distribuição manual de holerites)
- Definição de requisitos funcionais e não-funcionais
- Análise de conformidade com LGPD

**Etapa 2 — Modelagem Conceitual (Semana 1-2)**
- Criação do Diagrama Entidade-Relacionamento (DER)
- Identificação de entidades, atributos e relacionamentos
- Definição de cardinalidades (1:N, 1:1)

**Etapa 3 — Modelagem Lógica (Semana 2)**
- Normalização das tabelas até a 3ª Forma Normal (3FN)
- Definição de tipos de dados, constraints e índices
- Planejamento de enums e check constraints

**Etapa 4 — Modelagem Física e Implementação (Semana 2-3)**
- Criação das migrations via Entity Framework Core
- Implementação de 9 tabelas com todas as constraints
- Seed de dados iniciais (4 planos de assinatura)
- Implementação de Global Query Filters para multi-tenancy

**Etapa 5 — Desenvolvimento da Aplicação (Semana 3-4)**
- Desenvolvimento da API REST (.NET 8) com operações CRUD
- Implementação de criptografia AES-256 para PII
- Integração com WhatsApp (Evolution API) para notificações
- Desenvolvimento do frontend React

**Etapa 6 — Deploy em Nuvem (Semana 4)**
- Configuração do Docker Compose com PostgreSQL, Redis, MinIO
- Deploy em VPS via Dokploy (orquestrador de containers)
- Configuração de DNS, SSL (Let's Encrypt) e proxy reverso (Traefik)
- Testes de integração em ambiente de produção

### Ferramentas e Tecnologias Utilizadas

| Categoria | Tecnologia | Versão | Finalidade |
|-----------|-----------|--------|------------|
| **SGBD** | PostgreSQL | 16 (Alpine) | Banco de dados relacional principal |
| **ORM** | Entity Framework Core | 8.0 | Mapeamento objeto-relacional e migrations |
| **Backend** | .NET / C# | 8.0 | API REST, lógica de negócio |
| **Frontend** | React + TypeScript | 19 | Interface web responsiva |
| **Cache** | Redis | 7 (Alpine) | Cache, rate limiting, fila de jobs |
| **Storage** | MinIO | Latest | Armazenamento S3-compatível para PDFs |
| **WhatsApp** | Evolution API | 2.3.7 | Integração WhatsApp para notificações |
| **Container** | Docker + Docker Compose | Latest | Orquestração de serviços |
| **Deploy** | Dokploy | 0.26.7 | Orquestrador de deploy em VPS |
| **Proxy** | Traefik | v3 | Proxy reverso com SSL automático |
| **Versionamento** | Git + GitHub | — | Controle de versão |

### Banco de Dados em Nuvem

O banco de dados PostgreSQL 16 é executado em **nuvem**, dentro de um container Docker em um VPS (Virtual Private Server) com as seguintes características:

- **Provedor:** Vultr (VPS dedicado)
- **IP:** 209.61.36.86
- **Domínio:** th-sistema.sbs (com certificado SSL Let's Encrypt)
- **Persistência:** Volume Docker nomeado (`pgdata`) para garantir durabilidade dos dados mesmo em reinicializações de container
- **Rede:** Rede interna Docker (`hs-internal`) isolando os serviços; apenas o frontend/proxy é exposto externamente
- **Acesso:** O banco NÃO é exposto na internet — acessível apenas por serviços internos na rede Docker

O diagrama de infraestrutura:

```
Internet → Traefik (SSL) → Nginx (Frontend)
                                  ↓
                              API (.NET 8)
                            /    |    \     \
                     PostgreSQL Redis MinIO Evolution API
                       (BD)    (Cache) (S3)  (WhatsApp)
```

---

## 1.5 — Análise do Local

### Ambiente de Aplicação

A solução é aplicada em **empresas de pequeno e médio porte** que possuem funcionários regidos pela CLT e precisam distribuir holerites mensalmente. O ambiente digital substitui o processo físico anteriormente realizado em escritórios de RH/departamento pessoal.

### Necessidades Identificadas

1. **Eliminação de papel:** empresas gastam em média R$2-5 por holerite impresso e distribuído
2. **Comprovante de recebimento:** a legislação trabalhista exige comprovação de que o funcionário recebeu o holerite
3. **Proteção de dados sensíveis:** CPF e informações salariais precisam de tratamento adequado conforme LGPD
4. **Agilidade na distribuição:** funcionários em home office ou viagem não conseguem receber documentos físicos
5. **Auditoria e conformidade:** necessidade de rastro auditável de todas as operações

### Mapeamento do Ambiente

O sistema opera em três camadas:

| Camada | Descrição |
|--------|-----------|
| **Administrador (Empresa)** | Acessa o painel web para upload de holerites, gerenciamento de funcionários e acompanhamento de assinaturas |
| **Funcionário** | Recebe notificação via WhatsApp, acessa link de assinatura, verifica identidade e assina digitalmente |
| **Infraestrutura** | VPS em nuvem com Docker rodando PostgreSQL, API, frontend e serviços auxiliares |

> **Nota:** Fotos do local de aplicação (escritório da empresa cliente) devem ser incluídas na versão impressa/apresentação final do trabalho.

---

## 1.6 — Cronograma

### Atividades Planejadas

| Semana | Período | Atividade | Status |
|--------|---------|-----------|--------|
| 1 | 24/02 – 02/03 | Levantamento de requisitos e análise do problema | ✅ Concluído |
| 1-2 | 02/03 – 09/03 | Modelagem conceitual (DER) e modelagem lógica (3FN) | ✅ Concluído |
| 2 | 09/03 – 16/03 | Modelagem física: criação das migrations e tabelas no PostgreSQL | ✅ Concluído |
| 2-3 | 16/03 – 23/03 | Implementação das operações CRUD na API REST | ✅ Concluído |
| 3 | 16/03 – 23/03 | Implementação de segurança: criptografia AES-256, hash chain, multi-tenancy | ✅ Concluído |
| 3-4 | 23/03 – 28/03 | Deploy em nuvem (Docker + VPS) e testes de integração | ✅ Concluído |
| 4 | 28/03 – 30/03 | Elaboração da documentação completa | ✅ Concluído |
| 5 | 30/03 – 01/04 | Revisão final e preparação da apresentação | 🔄 Em andamento |
| — | 01/04/2026 | **Entrega e apresentação do projeto** | 📅 Agendado |

### Previsão de Problemas e Soluções

| Problema Potencial | Impacto | Solução Implementada |
|--------------------|---------|---------------------|
| Queda do VPS/servidor | Indisponibilidade total | Volumes Docker persistentes; redeploy automático via Dokploy |
| Perda de dados | Dados irrecuperáveis | Volumes nomeados (`pgdata`) com persistência; backup via `pg_dump` |
| Conflito de dados entre empresas | Vazamento de dados (LGPD) | Global Query Filters no EF Core — filtra automaticamente por `AdminId` |
| Sobrecarga do banco | Lentidão nas consultas | Índices otimizados em colunas frequentes; Redis para cache |
| Falha na criptografia | Exposição de PII | Chave AES-256 derivada via SHA-256, armazenada em variável de ambiente |
| Token de assinatura comprometido | Acesso não autorizado | Token com hash SHA-256 no banco + expiração temporal |
| Dados corrompidos na auditoria | Perda de integridade | Hash chain SHA-256 encadeado — qualquer alteração quebra a cadeia |

---

## 1.7 — Considerações Finais

### Expectativas em Relação à Aplicação

O projeto HoleriteSign demonstra na prática como um banco de dados bem projetado pode resolver problemas reais de negócios. As expectativas incluem:

1. **Eliminação completa do processo manual** de distribuição de holerites em papel, reduzindo custos operacionais em até 80%.

2. **Conformidade com a LGPD** através de criptografia de dados sensíveis diretamente no banco de dados, demonstrando que é possível armazenar PII de forma segura usando tipos nativos do PostgreSQL (`bytea`).

3. **Escalabilidade comprovada:** a arquitetura multi-tenant permite que o mesmo banco de dados atenda dezenas ou centenas de empresas, com isolamento total de dados garantido pelos Query Filters.

4. **Auditoria inviolável:** a cadeia de hash no `audit_logs` garante que qualquer tentativa de adulteração de registros seja detectável, algo crítico para conformidade trabalhista.

5. **Aplicação em produção real:** o sistema está disponível em `https://th-sistema.sbs` com dados reais de empresas clientes, demonstrando que a solução não é apenas acadêmica, mas funcional e aplicável.

### Potenciais Desafios e Estratégias

| Desafio | Estratégia |
|---------|-----------|
| Crescimento do volume de dados | Particionamento de tabelas (ex: `audit_logs` por mês) e arquivamento de dados antigos |
| Migração de schema sem downtime | Entity Framework Migrations com rollback; zero-downtime deploy via Dokploy |
| Backup e recuperação de desastres | Implementação de `pg_dump` periódico + replicação para outro VPS (fase futura) |
| Performance com muitas empresas | Índices compostos já implementados; suporte futuro a read replicas do PostgreSQL |
| Novas regulamentações de privacidade | Estrutura flexível — `event_data` em JSONB permite extensão sem alteração de schema |

---

## Apêndice A — Diagrama Entidade-Relacionamento (DER)

```
┌──────────────┐
│    plans     │
│──────────────│
│ PK id (uuid) │
│ name         │
│ display_name │
│ max_documents│
│ max_employees│
│ price_monthly│
│ is_active    │
│ created_at   │
└──────┬───────┘
       │ 1:N
       ▼
┌──────────────────┐          ┌────────────────────┐
│     admins       │          │    pay_periods     │
│──────────────────│          │────────────────────│
│ PK id (uuid)     │ 1:N     │ PK id (uuid)       │
│ FK plan_id       │────────►│ FK admin_id         │
│ name             │          │ year               │
│ email (UNIQUE)   │          │ month (CHECK 1-12) │
│ password_hash    │          │ label              │
│ company_name     │          │ created_at         │
│ role (CHECK)     │          └────────┬───────────┘
│ email_verified   │                   │ 1:N
│ is_active        │                   │
│ created_at       │                   │
│ updated_at       │                   │
│ refresh_token    │                   │
│ ...tokens...     │                   │
└──────┬───────────┘                   │
       │ 1:N                           │
       ▼                               │
┌──────────────────┐                   │
│   employees      │                   │
│──────────────────│                   │
│ PK id (uuid)     │                   │
│ FK admin_id      │                   │
│ name             │                   │
│ email            │                   │
│ whatsapp         │                   │
│ cpf_encrypted    │ (AES-256 bytea)   │
│ cpf_last4        │                   │
│ birth_date_enc.  │ (AES-256 bytea)   │
│ is_active        │                   │
│ deleted_at       │ (soft delete)     │
│ CHECK(contact)   │                   │
└──────┬───────────┘                   │
       │ 1:N                           │
       ▼                               ▼
┌──────────────────────────────────────────┐
│              documents                   │
│──────────────────────────────────────────│
│ PK id (uuid)                             │
│ FK employee_id                           │
│ FK pay_period_id                         │
│ FK admin_id                              │
│ original_filename                        │
│ original_file_key    (S3/MinIO)          │
│ original_file_hash   (SHA-256)           │
│ file_size_bytes                          │
│ signed_file_key      (S3/MinIO)          │
│ signed_file_hash     (SHA-256)           │
│ status (CHECK: uploaded/sent/signed/exp) │
│ signing_token_hash   (SHA-256, UNIQUE)   │
│ token_expires_at                         │
│ UNIQUE(employee_id, pay_period_id)       │
└──────┬──────────┬──────────┬─────────────┘
       │ 1:1      │ 1:N      │ 1:1
       ▼          ▼          ▼
┌───────────┐ ┌─────────────┐ ┌──────────────────────┐
│signatures │ │notifications│ │signing_verifications │
│───────────│ │─────────────│ │──────────────────────│
│PK id      │ │PK id        │ │PK id                 │
│FK doc_id  │ │FK doc_id    │ │FK document_id        │
│FK emp_id  │ │FK emp_id    │ │FK employee_id        │
│photo_key  │ │channel      │ │method (CHECK)        │
│photo_hash │ │status       │ │verified              │
│signer_ip  │ │external_id  │ │otp_hash (SHA-256)    │
│user_agent │ │error_message│ │otp_expires_at        │
│geoloc.    │ │sent_at      │ │attempt_count         │
│(jsonb)    │ │delivered_at │ │attempt_window_start  │
│consent    │ │read_at      │ │expires_at            │
│signed_at  │ │created_at   │ │created_at            │
│verif.meth │ │             │ │updated_at            │
└───────────┘ └─────────────┘ └──────────────────────┘

       ┌────────────────────────────────┐
       │         audit_logs             │
       │────────────────────────────────│
       │ PK id (bigint IDENTITY)        │
       │ admin_id (nullable)            │
       │ employee_id (nullable)         │
       │ document_id (nullable)         │
       │ event_type                     │
       │ event_data (jsonb)             │
       │ actor_type (CHECK)             │
       │ actor_ip                       │
       │ actor_user_agent               │
       │ prev_hash (SHA-256 chain)      │
       │ entry_hash (SHA-256)           │
       │ chain_version                  │
       │ created_at                     │
       └────────────────────────────────┘
```

---

## Apêndice B — Exemplos de Consultas SQL (CRUD)

### CREATE — Inserção de Dados

```sql
-- Inserir um novo administrador/empresa
INSERT INTO admins (id, name, email, password_hash, company_name, plan_id, role, created_at, updated_at)
VALUES (
    gen_random_uuid(),
    'Fernando Quintanilha',
    'fernando@empresa.com',
    '$2a$11$...hash_bcrypt...',
    'Empresa XYZ Ltda',
    (SELECT id FROM plans WHERE name = 'free'),
    'admin',
    NOW(),
    NOW()
);

-- Inserir um funcionário com CPF criptografado
INSERT INTO employees (id, admin_id, name, email, whatsapp, cpf_encrypted, cpf_last4, is_active, created_at, updated_at)
VALUES (
    gen_random_uuid(),
    'faff6322-72cc-4c83-86d5-5b049751fd8b',
    'Maria Silva',
    'maria@empresa.com',
    '+5562999999999',
    E'\\x...dados_criptografados_aes256...',  -- AES-256 encrypted
    '8901',  -- últimos 4 dígitos
    true,
    NOW(),
    NOW()
);

-- Inserir um período de pagamento
INSERT INTO pay_periods (id, admin_id, year, month, label, created_at)
VALUES (gen_random_uuid(), 'faff6322-...', 2026, 3, 'Março 2026', NOW());

-- Inserir um registro de auditoria com hash chain
INSERT INTO audit_logs (admin_id, document_id, event_type, event_data, actor_type, actor_ip, prev_hash, entry_hash, created_at)
VALUES (
    'faff6322-...',
    'doc-uuid-...',
    'document.uploaded',
    '{"filename": "holerite_marco.pdf", "size": 45231}'::jsonb,
    'admin',
    '189.50.123.45',
    'hash_sha256_do_registro_anterior',
    'hash_sha256_deste_registro',
    NOW()
);
```

### READ — Consultas de Dados

```sql
-- Listar funcionários de uma empresa com CPF mascarado
SELECT id, name, email, whatsapp,
       CASE WHEN cpf_last4 IS NOT NULL THEN '***.' || cpf_last4 ELSE NULL END AS cpf_masked,
       is_active, created_at
FROM employees
WHERE admin_id = 'faff6322-...'
  AND deleted_at IS NULL
ORDER BY name;

-- Dashboard: contar documentos por status
SELECT status, COUNT(*) as total
FROM documents
WHERE admin_id = 'faff6322-...'
GROUP BY status;

-- Relatório: funcionários com holerite pendente de assinatura em março/2026
SELECT e.name, e.email, e.whatsapp, d.status, d.created_at
FROM employees e
LEFT JOIN documents d ON d.employee_id = e.id
LEFT JOIN pay_periods pp ON d.pay_period_id = pp.id
WHERE e.admin_id = 'faff6322-...'
  AND pp.year = 2026 AND pp.month = 3
  AND d.status != 'signed'
  AND e.deleted_at IS NULL
ORDER BY e.name;

-- Consultar trilha de auditoria de um documento
SELECT id, event_type, actor_type, actor_ip, event_data, prev_hash, entry_hash, created_at
FROM audit_logs
WHERE document_id = 'doc-uuid-...'
ORDER BY created_at;

-- Verificar integridade da cadeia de auditoria
SELECT a1.id, a1.entry_hash, a2.prev_hash,
       CASE WHEN a1.entry_hash = a2.prev_hash THEN 'OK' ELSE 'VIOLAÇÃO!' END AS integridade
FROM audit_logs a1
JOIN audit_logs a2 ON a2.id = a1.id + 1
WHERE a1.document_id = 'doc-uuid-...'
ORDER BY a1.id;
```

### UPDATE — Alteração de Dados

```sql
-- Atualizar dados de um funcionário
UPDATE employees
SET name = 'Maria Silva Santos',
    email = 'maria.santos@empresa.com',
    whatsapp = '+5562988888888',
    updated_at = NOW()
WHERE id = 'emp-uuid-...'
  AND admin_id = 'faff6322-...';

-- Marcar documento como assinado
UPDATE documents
SET status = 'signed',
    signed_file_key = 'signed/doc-uuid-signed.pdf',
    signed_file_hash = 'sha256_do_pdf_assinado',
    token_used_at = NOW(),
    updated_at = NOW()
WHERE id = 'doc-uuid-...';

-- Atualizar status de notificação (webhook de entrega)
UPDATE notifications
SET status = 'delivered',
    delivered_at = NOW()
WHERE external_id = 'whatsapp-message-id-123';

-- Upgrade de plano de uma empresa
UPDATE admins
SET plan_id = (SELECT id FROM plans WHERE name = 'pro'),
    updated_at = NOW()
WHERE id = 'faff6322-...';
```

### DELETE — Exclusão de Dados

```sql
-- Soft delete (exclusão lógica) de funcionário — preserva integridade referencial
UPDATE employees
SET is_active = false,
    deleted_at = NOW(),
    updated_at = NOW()
WHERE id = 'emp-uuid-...'
  AND admin_id = 'faff6322-...';

-- Hard delete de notificações antigas (LGPD — retenção de dados)
DELETE FROM notifications
WHERE created_at < NOW() - INTERVAL '365 days'
  AND status IN ('sent', 'delivered', 'read');

-- Limpeza de tokens expirados
UPDATE documents
SET signing_token_hash = NULL,
    token_expires_at = NULL,
    status = 'expired',
    updated_at = NOW()
WHERE token_expires_at < NOW()
  AND status = 'sent';

-- Anonimização de funcionários inativos (LGPD — direito ao esquecimento)
UPDATE employees
SET name = 'Anonimizado',
    email = NULL,
    whatsapp = NULL,
    cpf_encrypted = NULL,
    cpf_last4 = NULL,
    birth_date_encrypted = NULL,
    updated_at = NOW()
WHERE deleted_at < NOW() - INTERVAL '730 days';
```

---

## Apêndice C — Schema Completo (DDL Resumido)

```sql
-- Tipos enumerados (armazenados como varchar com CHECK constraints)
-- AdminRole:        'admin', 'superadmin'
-- DocumentStatus:   'uploaded', 'sent', 'signed', 'expired'
-- NotificationChannel: 'email', 'whatsapp'
-- NotificationStatus:  'pending', 'sent', 'delivered', 'read', 'failed'
-- ActorType:        'admin', 'employee', 'system'
-- VerificationMethod: 'otp', 'cpf', 'dob'

CREATE TABLE plans (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name            VARCHAR(50) NOT NULL,
    display_name    VARCHAR(100) NOT NULL,
    max_documents   INTEGER NOT NULL DEFAULT 10,
    max_employees   INTEGER NOT NULL DEFAULT 5,
    price_monthly   DECIMAL(10,2) NOT NULL DEFAULT 0,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE admins (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name            VARCHAR(255) NOT NULL,
    email           VARCHAR(255) NOT NULL UNIQUE,
    password_hash   VARCHAR(255) NOT NULL,
    company_name    VARCHAR(255) NOT NULL,
    plan_id         UUID NOT NULL REFERENCES plans(id),
    role            VARCHAR(20) NOT NULL DEFAULT 'admin'
                    CHECK (role IN ('admin', 'superadmin')),
    email_verified  BOOLEAN NOT NULL DEFAULT FALSE,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    refresh_token               VARCHAR(128),
    refresh_token_expires_at    TIMESTAMPTZ,
    email_verification_token    VARCHAR(128),
    email_verification_expires_at TIMESTAMPTZ,
    password_reset_token        VARCHAR(128),
    password_reset_expires_at   TIMESTAMPTZ
);

CREATE TABLE employees (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    admin_id                UUID NOT NULL REFERENCES admins(id),
    name                    VARCHAR(255) NOT NULL,
    email                   VARCHAR(255),
    whatsapp                VARCHAR(20),
    cpf_encrypted           BYTEA,          -- AES-256 encrypted
    cpf_last4               CHAR(4),
    birth_date_encrypted    BYTEA,          -- AES-256 encrypted
    is_active               BOOLEAN NOT NULL DEFAULT TRUE,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at              TIMESTAMPTZ,    -- soft delete
    CONSTRAINT chk_contact CHECK (email IS NOT NULL OR whatsapp IS NOT NULL)
);

CREATE TABLE pay_periods (
    id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    admin_id    UUID NOT NULL REFERENCES admins(id),
    year        INTEGER NOT NULL,
    month       INTEGER NOT NULL CHECK (month BETWEEN 1 AND 12),
    label       VARCHAR(50),
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (admin_id, year, month)
);

CREATE TABLE documents (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    employee_id         UUID NOT NULL REFERENCES employees(id),
    pay_period_id       UUID NOT NULL REFERENCES pay_periods(id),
    admin_id            UUID NOT NULL REFERENCES admins(id),
    original_filename   VARCHAR(500) NOT NULL,
    original_file_key   VARCHAR(500) NOT NULL,
    original_file_hash  VARCHAR(64) NOT NULL,   -- SHA-256
    file_size_bytes     BIGINT NOT NULL,
    signed_file_key     VARCHAR(500),
    signed_file_hash    VARCHAR(64),            -- SHA-256
    status              VARCHAR(20) NOT NULL DEFAULT 'uploaded'
                        CHECK (status IN ('uploaded', 'sent', 'signed', 'expired')),
    signing_token_hash  CHAR(64) UNIQUE,        -- SHA-256 of raw token
    token_expires_at    TIMESTAMPTZ,
    token_used_at       TIMESTAMPTZ,
    viewed_at           TIMESTAMPTZ,
    last_notified_at    TIMESTAMPTZ,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (employee_id, pay_period_id)
);

CREATE TABLE signatures (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id             UUID NOT NULL UNIQUE REFERENCES documents(id),
    employee_id             UUID NOT NULL REFERENCES employees(id),
    photo_file_key          VARCHAR(500) NOT NULL,
    photo_hash              VARCHAR(64) NOT NULL,
    photo_mime_type         VARCHAR(50) NOT NULL,
    signer_ip               VARCHAR(45) NOT NULL,
    signer_user_agent       TEXT NOT NULL,
    signer_geolocation      JSONB,
    signer_device_info      JSONB,
    signed_at               TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    consent_given           BOOLEAN NOT NULL DEFAULT TRUE,
    consent_text            TEXT NOT NULL,
    verification_method     VARCHAR(10) CHECK (verification_method IN ('otp', 'cpf', 'dob')),
    verification_hash       CHAR(64),
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE notifications (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id     UUID NOT NULL REFERENCES documents(id),
    employee_id     UUID NOT NULL REFERENCES employees(id),
    channel         VARCHAR(20) NOT NULL,
    status          VARCHAR(20) NOT NULL DEFAULT 'pending',
    external_id     VARCHAR(255),
    error_message   TEXT,
    sent_at         TIMESTAMPTZ,
    delivered_at    TIMESTAMPTZ,
    read_at         TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE signing_verifications (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_id             UUID NOT NULL REFERENCES documents(id),
    employee_id             UUID NOT NULL REFERENCES employees(id),
    method                  VARCHAR(10) NOT NULL CHECK (method IN ('otp', 'cpf', 'dob')),
    verified                BOOLEAN NOT NULL DEFAULT FALSE,
    verified_at             TIMESTAMPTZ,
    otp_hash                CHAR(64),
    otp_expires_at          TIMESTAMPTZ,
    last_sent_at            TIMESTAMPTZ,
    attempt_count           INTEGER NOT NULL DEFAULT 0,
    attempt_window_start    TIMESTAMPTZ,
    expires_at              TIMESTAMPTZ NOT NULL,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE audit_logs (
    id              BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    admin_id        UUID,
    employee_id     UUID,
    document_id     UUID,
    event_type      VARCHAR(50) NOT NULL,
    event_data      JSONB,
    actor_type      VARCHAR(20) NOT NULL CHECK (actor_type IN ('admin', 'employee', 'system')),
    actor_ip        VARCHAR(45),
    actor_user_agent TEXT,
    prev_hash       CHAR(64),       -- SHA-256 do registro anterior
    entry_hash      CHAR(64) NOT NULL, -- SHA-256 deste registro
    chain_version   INTEGER NOT NULL DEFAULT 1,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Índices otimizados
CREATE INDEX idx_employees_admin ON employees(admin_id);
CREATE INDEX idx_documents_employee ON documents(employee_id);
CREATE INDEX idx_documents_period ON documents(pay_period_id);
CREATE UNIQUE INDEX idx_documents_token ON documents(signing_token_hash) WHERE signing_token_hash IS NOT NULL;
CREATE INDEX idx_documents_status ON documents(status);
CREATE INDEX idx_notifications_document ON notifications(document_id);
CREATE INDEX idx_audit_document ON audit_logs(document_id);
CREATE INDEX idx_audit_employee ON audit_logs(employee_id);
CREATE INDEX idx_audit_event ON audit_logs(event_type);
CREATE INDEX idx_audit_created ON audit_logs(created_at);
CREATE INDEX idx_signing_verifications_document ON signing_verifications(document_id);
CREATE UNIQUE INDEX idx_signing_verifications_active ON signing_verifications(document_id) WHERE verified = false;
```

---

## Apêndice D — Configuração Docker Compose (Infraestrutura)

```yaml
services:
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: holeritesign
      POSTGRES_USER: holeritesign
      POSTGRES_PASSWORD: [senha_segura]
    volumes:
      - pgdata:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U holeritesign"]
      interval: 10s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    command: redis-server --requirepass [senha_segura]

  minio:
    image: minio/minio
    command: server /data --console-address ":9001"
    volumes:
      - miniodata:/data

  api:
    build: ./backend
    depends_on:
      postgres: { condition: service_healthy }
      redis:    { condition: service_started }
      minio:    { condition: service_started }

  frontend:
    build: ./frontend
    depends_on:
      - api

volumes:
  pgdata:      # Persistência do PostgreSQL
  miniodata:   # Persistência do MinIO (PDFs)
  redis_data:  # Persistência do Redis
  evolution_data: # Persistência do WhatsApp
```

---

## 1.8 — Publicação no Medium

> **Nota:** Esta documentação deve ser publicada no site [https://medium.com/](https://medium.com/) conforme exigência do edital. Adaptar o conteúdo para formato de artigo, adicionando imagens das telas do sistema e do banco de dados em funcionamento.

**Sugestão de título para o artigo:**
> *"HoleriteSign: Projetando um Banco de Dados PostgreSQL para Gestão Digital de Holerites com Criptografia e Auditoria"*

**Estrutura sugerida para o Medium:**
1. Introdução ao problema (seção 1.1)
2. A solução: arquitetura do banco de dados (DER + tabelas)
3. Segurança: criptografia AES-256 e hash chain (demonstrar com exemplos SQL)
4. Multi-tenancy: como isolar dados de múltiplas empresas
5. Deploy em nuvem com Docker
6. Resultados e conclusão

---

## Referências Bibliográficas

1. PostgreSQL Global Development Group. **PostgreSQL 16 Documentation**. Disponível em: https://www.postgresql.org/docs/16/. Acesso em: março de 2026.

2. ELMASRI, R.; NAVATHE, S. B. **Sistemas de Banco de Dados**. 7ª ed. São Paulo: Pearson, 2019.

3. DATE, C. J. **Introdução a Sistemas de Banco de Dados**. 8ª ed. Rio de Janeiro: Campus, 2004.

4. BRASIL. **Lei nº 13.709, de 14 de agosto de 2018** (Lei Geral de Proteção de Dados Pessoais — LGPD). Disponível em: http://www.planalto.gov.br/ccivil_03/_ato2015-2018/2018/lei/l13709.htm. Acesso em: março de 2026.

5. Microsoft. **Entity Framework Core Documentation**. Disponível em: https://learn.microsoft.com/en-us/ef/core/. Acesso em: março de 2026.

6. Docker Inc. **Docker Compose Documentation**. Disponível em: https://docs.docker.com/compose/. Acesso em: março de 2026.

7. SILBERSCHATZ, A.; KORTH, H. F.; SUDARSHAN, S. **Sistema de Banco de Dados**. 7ª ed. São Paulo: GEN LTC, 2020.

8. NIST. **Advanced Encryption Standard (AES) — FIPS PUB 197**. National Institute of Standards and Technology, 2001. Disponível em: https://csrc.nist.gov/publications/detail/fips/197/final.
