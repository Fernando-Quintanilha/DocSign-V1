import { useParams, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { fetchEmployeeDetail, fetchDocuments, fetchAuditLogs } from '../../services/api';
import type { Document, AuditLogDto } from '../../types';

const statusLabels: Record<string, { label: string; color: string }> = {
  Uploaded: { label: 'Enviado', color: 'bg-blue-100 text-blue-800' },
  Sent: { label: 'Notificado', color: 'bg-yellow-100 text-yellow-800' },
  Signed: { label: 'Assinado', color: 'bg-green-100 text-green-800' },
  Expired: { label: 'Expirado', color: 'bg-red-100 text-red-800' },
};

const eventTypeLabels: Record<string, string> = {
  DocumentUploaded: 'Documento enviado',
  DocumentSigned: 'Documento assinado',
  DocumentDeleted: 'Documento excluído',
  NotificationSent: 'Notificação enviada',
  SigningTokenGenerated: 'Link de assinatura gerado',
  IdentityVerified: 'Identidade verificada',
  EmployeeCreated: 'Funcionário criado',
  EmployeeUpdated: 'Funcionário atualizado',
};

export default function EmployeeDetailPage() {
  const { id } = useParams<{ id: string }>();

  const { data: employee } = useQuery({
    queryKey: ['employee', id],
    queryFn: () => fetchEmployeeDetail(id!),
    enabled: !!id,
  });

  const { data: documents = [], isLoading: loadingDocs } = useQuery({
    queryKey: ['employee-documents', id],
    queryFn: () => fetchDocuments({ employeeId: id }),
    enabled: !!id,
  });

  const { data: auditData } = useQuery({
    queryKey: ['employee-audit', id],
    queryFn: () => fetchAuditLogs({ employeeId: id, pageSize: 20 }),
    enabled: !!id,
  });

  const formatDate = (d: string) => new Date(d).toLocaleDateString('pt-BR');
  const formatDateTime = (d: string) =>
    new Date(d).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' });

  if (!employee) {
    return (
      <div className="text-center py-12">
        <p className="text-gray-500">Funcionário não encontrado.</p>
        <Link to="/admin/employees" className="text-primary-600 hover:underline text-sm mt-2 inline-block">
          ← Voltar para lista
        </Link>
      </div>
    );
  }

  const signed = documents.filter((d: Document) => d.status === 'Signed').length;
  const pending = documents.filter((d: Document) => d.status !== 'Signed').length;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center gap-3">
        <Link to="/admin/employees" className="text-gray-400 hover:text-gray-600">
          ← 
        </Link>
        <div>
          <h1 className="text-2xl font-bold text-gray-900">{employee.name}</h1>
          <p className="text-sm text-gray-500">{employee.email || employee.whatsApp || 'Sem contato'}</p>
        </div>
        <span className={`ml-auto inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
          employee.isActive ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'
        }`}>
          {employee.isActive ? 'Ativo' : 'Inativo'}
        </span>
      </div>

      {/* Info Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div className="bg-white rounded-lg shadow p-4">
          <p className="text-xs text-gray-500 uppercase">CPF</p>
          <p className="text-lg font-semibold text-gray-900">
            {employee.cpfLast4 ? `***.***.***-${employee.cpfLast4}` : '—'}
          </p>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <p className="text-xs text-gray-500 uppercase">Total Documentos</p>
          <p className="text-lg font-semibold text-gray-900">{documents.length}</p>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <p className="text-xs text-gray-500 uppercase">Assinados</p>
          <p className="text-lg font-semibold text-green-600">{signed}</p>
        </div>
        <div className="bg-white rounded-lg shadow p-4">
          <p className="text-xs text-gray-500 uppercase">Pendentes</p>
          <p className="text-lg font-semibold text-yellow-600">{pending}</p>
        </div>
      </div>

      {/* Documents Table */}
      <div className="bg-white rounded-lg shadow overflow-x-auto">
        <div className="px-6 py-4 border-b border-gray-200">
          <h2 className="text-lg font-semibold text-gray-900">Documentos</h2>
        </div>
        <table className="min-w-[600px] w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Período</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Arquivo</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Status</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Data</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200">
            {loadingDocs ? (
              <tr><td colSpan={4} className="px-6 py-8 text-center text-gray-500">Carregando...</td></tr>
            ) : documents.length === 0 ? (
              <tr><td colSpan={4} className="px-6 py-8 text-center text-gray-500">Nenhum documento.</td></tr>
            ) : (
              documents.map((doc: Document) => {
                const st = statusLabels[doc.status] || { label: doc.status, color: 'bg-gray-100 text-gray-800' };
                return (
                  <tr key={doc.id} className="hover:bg-gray-50">
                    <td className="px-6 py-3 text-sm text-gray-900">{doc.payPeriodLabel}</td>
                    <td className="px-6 py-3 text-sm text-gray-500">{doc.originalFilename}</td>
                    <td className="px-6 py-3">
                      <span className={`inline-flex px-2 py-0.5 text-xs font-semibold rounded-full ${st.color}`}>
                        {st.label}
                      </span>
                    </td>
                    <td className="px-6 py-3 text-sm text-gray-500">
                      {formatDate(doc.createdAt)}
                      {doc.signedAt && (
                        <span className="block text-xs text-green-600">Assinado: {formatDate(doc.signedAt)}</span>
                      )}
                    </td>
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>

      {/* Audit Timeline */}
      {auditData && auditData.data.length > 0 && (
        <div className="bg-white rounded-lg shadow">
          <div className="px-6 py-4 border-b border-gray-200">
            <h2 className="text-lg font-semibold text-gray-900">Atividade Recente</h2>
          </div>
          <div className="divide-y divide-gray-100">
            {auditData.data.map((log: AuditLogDto) => (
              <div key={log.id} className="px-6 py-3 flex items-center gap-3">
                <div className="w-2 h-2 rounded-full bg-primary-400 flex-shrink-0" />
                <div className="flex-1">
                  <p className="text-sm text-gray-800">
                    {eventTypeLabels[log.eventType] || log.eventType}
                  </p>
                  {log.documentId && (
                    <p className="text-xs text-gray-400">Doc: {log.documentId.slice(0, 8)}...</p>
                  )}
                </div>
                <span className="text-xs text-gray-400 flex-shrink-0">{formatDateTime(log.createdAt)}</span>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
