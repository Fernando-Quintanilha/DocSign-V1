import { useState } from 'react';
import { useQuery, useMutation } from '@tanstack/react-query';
import { fetchEnhancedDashboard, fetchPendingEmployees, sendNotification } from '../../services/api';
import type { PendingEmployee, PeriodSummary, RecentActivity } from '../../types';

const eventLabels: Record<string, string> = {
  token_generated: 'Token gerado',
  identity_verified: 'Identidade verificada',
  identity_verification_failed: 'Verificação falhou',
  document_signed: 'Documento assinado',
  notification_sent: 'Notificação enviada',
  notification_failed: 'Notificação falhou',
  document_uploaded: 'Documento enviado',
};

function ProgressBar({ signed, total }: { signed: number; total: number }) {
  const pct = total > 0 ? Math.round((signed / total) * 100) : 0;
  return (
    <div className="w-full bg-gray-200 rounded-full h-3 overflow-hidden">
      <div
        role="progressbar"
        aria-valuenow={pct}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-label={`${pct}% assinados`}
        title={`${signed} de ${total} assinados`}
        className={`h-full rounded-full transition-all ${pct === 100 ? 'bg-green-500' : 'bg-blue-500'}`}
        style={{ width: `${pct}%` }}
      />
    </div>
  );
}

export default function DashboardPage() {
  const [selectedPeriod, setSelectedPeriod] = useState('');

  const { data, isLoading } = useQuery({
    queryKey: ['dashboard-enhanced'],
    queryFn: fetchEnhancedDashboard,
  });

  // When user selects a specific period, fetch pending for that period
  const { data: filteredPending } = useQuery({
    queryKey: ['pending-by-period', selectedPeriod],
    queryFn: () => fetchPendingEmployees(selectedPeriod),
    enabled: !!selectedPeriod,
  });

  const sendNotifMutation = useMutation({
    mutationFn: ({ docId, channel }: { docId: string; channel: string }) =>
      sendNotification(docId, channel),
  });

  if (isLoading || !data) {
    return (
      <div className="flex items-center justify-center py-12">
        <p className="text-gray-500">Carregando...</p>
      </div>
    );
  }

  const usageMaxLabel = data.planMaxDocuments === -1 ? '∞' : data.planMaxDocuments;
  const usageMaxNum = data.planMaxDocuments === -1 ? 100 : data.planMaxDocuments;
  const usagePct = data.planMaxDocuments > 0
    ? Math.round((data.documentsUsedThisMonth / data.planMaxDocuments) * 100)
    : 0;

  const cards = [
    { label: 'Funcionários Ativos', value: data.activeEmployees, total: data.planMaxEmployees === -1 ? '∞' : data.planMaxEmployees },
    { label: 'Documentos Pendentes', value: data.pendingDocuments, color: 'text-yellow-600' },
    { label: 'Assinados', value: data.signedDocuments, color: 'text-green-600' },
    { label: 'Expirados', value: data.expiredDocuments, color: 'text-red-600' },
    { label: 'Total de Documentos', value: data.totalDocuments },
  ];

  return (
    <div className="space-y-8">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>
        <p className="mt-1 text-sm text-gray-500">
          Plano: <span className="font-medium text-blue-600">{data.planName}</span>
        </p>
      </div>

      {/* Stat Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-4">
        {cards.map((card) => (
          <div key={card.label} className="bg-white rounded-lg shadow p-5">
            <p className="text-sm font-medium text-gray-500">{card.label}</p>
            <p className={`mt-2 text-3xl font-bold ${card.color ?? 'text-gray-900'}`}>
              {card.value}
              {card.total !== undefined && (
                <span className="text-sm font-normal text-gray-400 ml-1">/ {card.total}</span>
              )}
            </p>
          </div>
        ))}
      </div>

      {/* Usage Meter (DASH-04 / PLAN-04) */}
      <div className="bg-white rounded-lg shadow p-6">
        <div className="flex items-center justify-between mb-2">
          <h2 className="text-lg font-semibold text-gray-900">Uso do Plano (este mês)</h2>
          <span className="text-sm text-gray-500">
            {data.documentsUsedThisMonth} / {usageMaxLabel} documentos enviados
          </span>
        </div>
        <div className="w-full bg-gray-200 rounded-full h-4 overflow-hidden">
          <div
            role="progressbar"
            aria-valuenow={data.documentsUsedThisMonth}
            aria-valuemin={0}
            aria-valuemax={usageMaxNum}
            aria-label="Uso do plano"
            title={`${data.documentsUsedThisMonth} de ${usageMaxLabel} documentos`}
            className={`h-full rounded-full transition-all ${usagePct >= 90 ? 'bg-red-500' : usagePct >= 70 ? 'bg-yellow-500' : 'bg-blue-500'}`}
            style={{ width: `${Math.min(usagePct, 100)}%` }}
          />
        </div>
        {usagePct >= 90 && data.planMaxDocuments > 0 && (
          <p className="mt-2 text-sm text-red-600 font-medium">
            ⚠ Você está perto do limite do seu plano. Considere fazer upgrade.
          </p>
        )}
      </div>

      {/* Two Column Layout */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">

        {/* Quem falta assinar (DASH-06) */}
        <div className="bg-white rounded-lg shadow p-6">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-semibold text-gray-900">
              Quem falta assinar
            </h2>
            {data.periods.length > 0 && (
              <select
                aria-label="Filtrar por período"
                value={selectedPeriod}
                onChange={(e) => setSelectedPeriod(e.target.value)}
                className="text-sm border border-gray-300 rounded-md px-2 py-1"
              >
                <option value="">Último período</option>
                {data.periods.map((p: PeriodSummary) => (
                  <option key={p.id} value={p.id}>{p.label}</option>
                ))}
              </select>
            )}
          </div>

          {(() => {
            const pending = selectedPeriod && filteredPending ? filteredPending : data.pendingEmployees;
            return pending.length === 0 ? (
              <p className="text-sm text-green-600 font-medium">
                ✓ Todos os funcionários assinaram!
              </p>
            ) : (
              <ul className="divide-y divide-gray-100 max-h-80 overflow-y-auto">
                {pending.map((emp: PendingEmployee) => (
                <li key={emp.employeeId} className="py-3 flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium text-gray-900">{emp.employeeName}</p>
                    <p className="text-xs text-gray-500">
                      {emp.documentStatus === 'NoDocument'
                        ? 'Sem documento'
                        : emp.documentStatus === 'Uploaded'
                          ? 'Aguardando envio'
                          : emp.documentStatus === 'Sent'
                            ? 'Notificado'
                            : emp.documentStatus === 'Expired'
                              ? 'Expirado'
                              : emp.documentStatus}
                      {emp.lastNotifiedAt && ` · Notificado em ${new Date(emp.lastNotifiedAt).toLocaleDateString('pt-BR')}`}
                    </p>
                  </div>
                  {emp.documentId && emp.documentStatus !== 'NoDocument' && (
                    <div className="relative inline-block group">
                      <button
                        disabled={sendNotifMutation.isPending}
                        className="text-xs bg-blue-50 text-blue-700 hover:bg-blue-100 px-3 py-1.5 rounded-md font-medium"
                      >
                        Lembrete ▾
                      </button>
                      <div className="absolute right-0 mt-1 w-36 bg-white border border-gray-200 rounded-lg shadow-lg opacity-0 invisible group-hover:opacity-100 group-hover:visible transition-all z-10">
                        <button
                          onClick={() => sendNotifMutation.mutate({ docId: emp.documentId!, channel: 'Email' })}
                          className="block w-full text-left px-4 py-2 text-xs text-gray-700 hover:bg-gray-100 rounded-t-lg"
                        >
                          📧 E-mail
                        </button>
                        <button
                          onClick={() => sendNotifMutation.mutate({ docId: emp.documentId!, channel: 'WhatsApp' })}
                          className="block w-full text-left px-4 py-2 text-xs text-gray-700 hover:bg-gray-100 rounded-b-lg"
                        >
                          💬 WhatsApp
                        </button>
                      </div>
                    </div>
                  )}
                </li>
              ))}
            </ul>
          );
          })()}
        </div>

        {/* Atividade Recente (DASH-05) */}
        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Atividade Recente</h2>

          {data.recentActivity.length === 0 ? (
            <p className="text-sm text-gray-500">Nenhuma atividade registrada.</p>
          ) : (
            <ul className="divide-y divide-gray-100 max-h-80 overflow-y-auto">
              {data.recentActivity.map((act: RecentActivity) => (
                <li key={act.id} className="py-3">
                  <div className="flex items-center justify-between">
                    <p className="text-sm text-gray-900">
                      {eventLabels[act.eventType] ?? act.eventType}
                    </p>
                    <span className="text-xs text-gray-400">
                      {new Date(act.createdAt).toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' })}
                    </span>
                  </div>
                  <p className="text-xs text-gray-500 mt-0.5">
                    {act.employeeName && <span>{act.employeeName}</span>}
                    {act.employeeName && act.documentFilename && <span> · </span>}
                    {act.documentFilename && <span>{act.documentFilename}</span>}
                    {!act.employeeName && !act.documentFilename && <span>{act.actorType}</span>}
                  </p>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>

      {/* Progress per Period (DASH-07) */}
      {data.periods.length > 0 && (
        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Progresso por Período</h2>
          <div className="space-y-4">
            {data.periods.map((period: PeriodSummary) => (
              <div key={period.id}>
                <div className="flex items-center justify-between mb-1">
                  <span className="text-sm font-medium text-gray-700">{period.label}</span>
                  <span className="text-sm text-gray-500">
                    {period.signedDocuments}/{period.totalDocuments} assinatura{period.totalDocuments !== 1 ? 's' : ''}
                  </span>
                </div>
                <ProgressBar signed={period.signedDocuments} total={period.totalDocuments} />
                {period.expiredDocuments > 0 && (
                  <p className="text-xs text-red-500 mt-1">{period.expiredDocuments} expirado(s)</p>
                )}
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
