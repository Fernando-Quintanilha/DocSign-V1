export interface Employee {
  id: string;
  name: string;
  email: string | null;
  whatsApp: string | null;
  cpfLast4: string | null;
  hasBirthDate: boolean;
  isActive: boolean;
  createdAt: string;
}

export interface Document {
  id: string;
  employeeId: string;
  employeeName: string;
  payPeriodId: string;
  payPeriodLabel: string;
  originalFilename: string;
  fileSizeBytes: number;
  status: 'Uploaded' | 'Sent' | 'Signed' | 'Expired';
  viewedAt: string | null;
  lastNotifiedAt: string | null;
  createdAt: string;
  signedAt: string | null;
}

export interface PayPeriod {
  id: string;
  year: number;
  month: number;
  label: string;
  documentCount: number;
  createdAt: string;
}

export interface DashboardStats {
  totalEmployees: number;
  activeEmployees: number;
  totalDocuments: number;
  pendingDocuments: number;
  signedDocuments: number;
  expiredDocuments: number;
  planName: string;
  planMaxEmployees: number;
  planMaxDocuments: number;
}

// ── Enhanced Dashboard (Phase 7) ──────────────────────────

export interface EnhancedDashboard {
  totalEmployees: number;
  activeEmployees: number;
  totalDocuments: number;
  pendingDocuments: number;
  signedDocuments: number;
  expiredDocuments: number;
  planName: string;
  planMaxEmployees: number;
  planMaxDocuments: number;
  documentsUsedThisMonth: number;
  periods: PeriodSummary[];
  pendingEmployees: PendingEmployee[];
  recentActivity: RecentActivity[];
}

export interface PeriodSummary {
  id: string;
  year: number;
  month: number;
  label: string;
  totalDocuments: number;
  signedDocuments: number;
  pendingDocuments: number;
  expiredDocuments: number;
}

export interface PendingEmployee {
  employeeId: string;
  employeeName: string;
  email: string | null;
  whatsApp: string | null;
  documentId: string | null;
  documentStatus: string;
  lastNotifiedAt: string | null;
}

export interface RecentActivity {
  id: number;
  eventType: string;
  actorType: string;
  employeeName: string | null;
  documentFilename: string | null;
  createdAt: string;
}

// ── Signing Flow ──────────────────────────────────────────

export interface GenerateTokenResponse {
  signingUrl: string;
  expiresAt: string;
}

export interface ValidateTokenResponse {
  valid: boolean;
  employeeName: string;
  companyName: string;
  payPeriodLabel: string;
  requiresCpf: boolean;
  requiresBirthDate: boolean;
}

export interface VerifyIdentityResponse {
  verified: boolean;
  message: string | null;
}

export interface SigningDocument {
  documentId: string;
  employeeName: string;
  companyName: string;
  payPeriodLabel: string;
  originalFilename: string;
  fileSizeBytes: number;
  downloadUrl: string;
}

export interface SignDocumentResponse {
  success: boolean;
  message: string;
  signedAt: string;
}

// ── Notifications ─────────────────────────────────────────

export interface NotificationDto {
  id: string;
  documentId: string;
  employeeName: string;
  channel: string;
  status: string;
  sentAt: string | null;
  errorMessage: string | null;
  createdAt: string;
}

// ── Audit ─────────────────────────────────────────────────

export interface AuditLogDto {
  id: number;
  eventType: string;
  actorType: string;
  actorIp: string | null;
  adminId: string | null;
  employeeId: string | null;
  documentId: string | null;
  eventData: string | null;
  createdAt: string;
}

export interface AuditLogPage {
  data: AuditLogDto[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

// ── Admin Profile ─────────────────────────────────────────

export interface AdminProfile {
  id: string;
  name: string;
  email: string;
  companyName: string;
  role: string;
  planName: string;
  emailVerified: boolean;
}

// ── WhatsApp ──────────────────────────────────────────────

export interface WhatsAppQrCode {
  pairingCode: string | null;
  code: string | null;
  base64: string | null;
  count: number;
}

export interface WhatsAppStatus {
  instance: {
    instanceName: string;
    instanceId: string;
    status: string;
  } | null;
  state: string;
}

export interface WhatsAppCreateInstanceResponse {
  instance: {
    instanceName: string;
    instanceId: string;
    status: string;
  } | null;
  hash: string | null;
  qrcode: {
    pairingCode: string | null;
    code: string | null;
    base64: string | null;
  } | null;
}
