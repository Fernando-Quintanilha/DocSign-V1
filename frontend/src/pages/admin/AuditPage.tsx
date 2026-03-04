import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { fetchAuditLogs } from '../../services/api';

const eventLabels: Record<string, string> = {
  token_generated: 'Token gerado',
  identity_verified: 'Identidade verificada',
  identity_verification_failed: 'Verificação falhou',
  document_signed: 'Documento assinado',
  notification_sent: 'Notificação enviada',
  notification_failed: 'Notificação falhou',
};

const actorLabels: Record<string, { label: string; color: string }> = {
  Admin: { label: 'Admin', color: 'bg-blue-100 text-blue-800' },
  Employee: { label: 'Funcionário', color: 'bg-green-100 text-green-800' },
  System: { label: 'Sistema', color: 'bg-gray-100 text-gray-800' },
};

export default function AuditPage() {
  const [page, setPage] = useState(1);
  const [eventType, setEventType] = useState('');

  const { data, isLoading } = useQuery({
    queryKey: ['audit-logs', page, eventType],
    queryFn: () =>
      fetchAuditLogs({
        page,
        pageSize: 25,
        eventType: eventType || undefined,
      }),
  });

  const formatDate = (dateStr: string) =>
    new Date(dateStr).toLocaleString('pt-BR');

  return (
    <div>
      <div className="flex items-center justify-between flex-wrap gap-2">
        <h1 className="text-2xl font-bold text-gray-900">Auditoria</h1>
        <select
          aria-label="Filtrar por tipo de evento"
          value={eventType}
          onChange={(e) => {
            setEventType(e.target.value);
            setPage(1);
          }}
          className="rounded-md border border-gray-300 px-3 py-2 text-sm shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
        >
          <option value="">Todos os eventos</option>
          <option value="token_generated">Token gerado</option>
          <option value="identity_verified">Identidade verificada</option>
          <option value="identity_verification_failed">Verificação falhou</option>
          <option value="document_signed">Documento assinado</option>
          <option value="notification_sent">Notificação enviada</option>
          <option value="notification_failed">Notificação falhou</option>
        </select>
      </div>

      <div className="mt-6 bg-white shadow rounded-lg overflow-x-auto">
        <table className="min-w-[800px] w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Data/Hora
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Evento
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Ator
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                IP
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Detalhes
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {isLoading ? (
              <tr>
                <td colSpan={5} className="px-6 py-12 text-center text-gray-500">
                  Carregando...
                </td>
              </tr>
            ) : !data || data.data.length === 0 ? (
              <tr>
                <td colSpan={5} className="px-6 py-12 text-center text-gray-500">
                  Nenhum registro de auditoria encontrado.
                </td>
              </tr>
            ) : (
              data.data.map((log) => {
                const actor = actorLabels[log.actorType] || {
                  label: log.actorType,
                  color: 'bg-gray-100 text-gray-800',
                };
                return (
                  <tr key={log.id}>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {formatDate(log.createdAt)}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                      {eventLabels[log.eventType] || log.eventType}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <span
                        className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${actor.color}`}
                      >
                        {actor.label}
                      </span>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 font-mono">
                      {log.actorIp || '—'}
                    </td>
                    <td className="px-6 py-4 text-sm text-gray-500 max-w-xs truncate">
                      {log.eventData ? (
                        <code className="text-xs bg-gray-50 px-2 py-1 rounded">
                          {log.eventData.length > 60
                            ? log.eventData.slice(0, 60) + '...'
                            : log.eventData}
                        </code>
                      ) : (
                        '—'
                      )}
                    </td>
                  </tr>
                );
              })
            )}
          </tbody>
        </table>

        {/* Pagination */}
        {data && data.totalPages > 1 && (
          <div className="px-6 py-3 flex items-center justify-between flex-wrap gap-2 border-t border-gray-200 bg-gray-50">
            <p className="text-sm text-gray-500">
              Página {data.page} de {data.totalPages} ({data.total} registros)
            </p>
            <div className="flex gap-2">
              <button
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page === 1}
                className="px-3 py-1 text-sm rounded-md border border-gray-300 hover:bg-gray-100 disabled:opacity-50"
              >
                Anterior
              </button>
              <button
                onClick={() => setPage((p) => Math.min(data.totalPages, p + 1))}
                disabled={page >= data.totalPages}
                className="px-3 py-1 text-sm rounded-md border border-gray-300 hover:bg-gray-100 disabled:opacity-50"
              >
                Próxima
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
