import api from '../lib/api';
import type {
  Employee,
  EmployeeDetail,
  Document,
  EnhancedDashboard,
  PendingEmployee,
  PayPeriod,
  GenerateTokenResponse,
  ValidateTokenResponse,
  VerifyIdentityResponse,
  SigningDocument,
  SignDocumentResponse,
  NotificationDto,
  AuditLogPage,
  AdminProfile,
  WhatsAppQrCode,
  WhatsAppStatus,
  WhatsAppCreateInstanceResponse,
} from '../types';

// ── Employees ─────────────────────────────────────────────

export const fetchEmployees = async (search?: string): Promise<Employee[]> => {
  const { data } = await api.get('/employees', { params: search ? { search } : undefined });
  return data;
};

export const fetchEmployeeDetail = async (id: string): Promise<EmployeeDetail> => {
  const { data } = await api.get(`/employees/${id}`);
  return data;
};

export const createEmployee = async (payload: {
  name: string;
  email?: string;
  whatsApp?: string;
  cpf?: string;
  birthDate?: string;
}): Promise<Employee> => {
  const { data } = await api.post('/employees', payload);
  return data;
};

export const updateEmployee = async (
  id: string,
  payload: {
    name: string;
    email?: string;
    whatsApp?: string;
    cpf?: string;
    birthDate?: string;
  }
): Promise<Employee> => {
  const { data } = await api.put(`/employees/${id}`, payload);
  return data;
};

export const deleteEmployee = async (id: string): Promise<void> => {
  await api.delete(`/employees/${id}`);
};

export const downloadDocument = async (id: string, type: 'original' | 'signed' = 'original'): Promise<Blob> => {
  const { data } = await api.get(`/documents/${id}/download`, {
    params: { type },
    responseType: 'blob',
  });
  return data;
};

export const fetchPayPeriods = async (): Promise<PayPeriod[]> => {
  const { data } = await api.get('/payperiods');
  return data;
};

// ── Documents ─────────────────────────────────────────────

export const fetchDocuments = async (params?: {
  employeeId?: string;
  payPeriodId?: string;
}): Promise<Document[]> => {
  const { data } = await api.get('/documents', { params });
  return data;
};

export const uploadDocument = async (
  file: File,
  employeeId: string,
  year: number,
  month: number
): Promise<Document> => {
  const formData = new FormData();
  formData.append('file', file);
  formData.append('employeeId', employeeId);
  formData.append('year', year.toString());
  formData.append('month', month.toString());
  const { data } = await api.post('/documents/upload', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
  return data;
};

export const deleteDocument = async (id: string): Promise<void> => {
  await api.delete(`/documents/${id}`);
};

// ── Dashboard ─────────────────────────────────────────────

export const fetchEnhancedDashboard = async (): Promise<EnhancedDashboard> => {
  const { data } = await api.get('/dashboard/enhanced');
  return data;
};

export const fetchPendingEmployees = async (payPeriodId: string): Promise<PendingEmployee[]> => {
  const { data } = await api.get('/dashboard/pending', { params: { payPeriodId } });
  return data;
};

// ── Export / Backup ───────────────────────────────────────

export const exportSignedPdfsZip = async (payPeriodId: string): Promise<Blob> => {
  const { data } = await api.get(`/export/period/${payPeriodId}`, { responseType: 'blob' });
  return data;
};

export const exportEmployeesCsv = async (): Promise<Blob> => {
  const { data } = await api.get('/export/employees', { responseType: 'blob' });
  return data;
};

export const exportAuditLogsCsv = async (): Promise<Blob> => {
  const { data } = await api.get('/export/audit-logs', { responseType: 'blob' });
  return data;
};

export const exportAllData = async (): Promise<Blob> => {
  const { data } = await api.get('/export/all', { responseType: 'blob' });
  return data;
};

// ── Employee CSV Import ───────────────────────────────────

export const importEmployeesCsv = async (file: File): Promise<{ created: number; skipped: number; errors: string[] }> => {
  const formData = new FormData();
  formData.append('file', file);
  const { data } = await api.post('/employees/import', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
  return data;
};

// ── Document Replace ──────────────────────────────────────

export const replaceDocument = async (documentId: string, file: File): Promise<any> => {
  const formData = new FormData();
  formData.append('file', file);
  const { data } = await api.put(`/documents/${documentId}/replace`, formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
  return data;
};

// ── Signing ───────────────────────────────────────────────

export const generateSigningToken = async (documentId: string): Promise<GenerateTokenResponse> => {
  const { data } = await api.post(`/documents/${documentId}/generate-token`);
  return data;
};

export const validateSigningToken = async (token: string): Promise<ValidateTokenResponse> => {
  const { data } = await api.get(`/signing/validate/${token}`);
  return data;
};

export const verifyIdentity = async (
  token: string,
  payload: { cpf?: string; birthDate?: string }
): Promise<VerifyIdentityResponse> => {
  const { data } = await api.post(`/signing/verify/${token}`, payload);
  return data;
};

export const getSigningDocument = async (token: string): Promise<SigningDocument> => {
  const { data } = await api.get(`/signing/document/${token}`);
  return data;
};

export const signDocument = async (
  token: string,
  payload: {
    photoBase64: string;
    photoMimeType: string;
    consentGiven: boolean;
    geolocation?: string;
  }
): Promise<SignDocumentResponse> => {
  const { data } = await api.post(`/signing/sign/${token}`, payload);
  return data;
};

// ── Notifications ─────────────────────────────────────────

export const sendNotification = async (
  documentId: string,
  channel: string
): Promise<NotificationDto> => {
  const { data } = await api.post('/notifications/send', { documentId, channel });
  return data;
};

// ── Audit ─────────────────────────────────────────────────

export const fetchAuditLogs = async (params?: {
  documentId?: string;
  employeeId?: string;
  eventType?: string;
  page?: number;
  pageSize?: number;
}): Promise<AuditLogPage> => {
  const { data } = await api.get('/audit', { params });
  return data;
};

// ── Auth/Profile ──────────────────────────────────────────

export const changePassword = async (currentPassword: string, newPassword: string): Promise<void> => {
  await api.post('/auth/change-password', { currentPassword, newPassword });
};

export const updateProfile = async (name: string, companyName: string): Promise<AdminProfile> => {
  const { data } = await api.put<AdminProfile>('/auth/profile', { name, companyName });
  return data;
};

export const fetchProfile = async (): Promise<AdminProfile> => {
  const { data } = await api.get<AdminProfile>('/auth/profile');
  return data;
};

// ── Plans ─────────────────────────────────────────────────

export const fetchPlans = async () => {
  const { data } = await api.get('/plans');
  return data;
};

export const fetchCurrentPlan = async () => {
  const { data } = await api.get('/plans/current');
  return data;
};

// ── WhatsApp ─────────────────────────────────────────────

export const createWhatsAppInstance = async (): Promise<WhatsAppCreateInstanceResponse> => {
  const { data } = await api.post('/notifications/whatsapp/create-instance');
  return data;
};

export const fetchWhatsAppQrCode = async (): Promise<WhatsAppQrCode> => {
  const { data } = await api.get('/notifications/whatsapp/qrcode');
  return data;
};

export const fetchWhatsAppStatus = async (): Promise<WhatsAppStatus> => {
  const { data } = await api.get('/notifications/whatsapp/status');
  return data;
};

export const logoutWhatsApp = async (): Promise<void> => {
  await api.delete('/notifications/whatsapp/logout');
};
