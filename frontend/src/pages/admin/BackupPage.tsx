import { useState } from 'react';
import { useQuery, useMutation } from '@tanstack/react-query';
import { fetchPayPeriods, exportSignedPdfsZip, exportEmployeesCsv, exportAuditLogsCsv, exportAllData } from '../../services/api';

function downloadBlob(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);
}

export default function BackupPage() {
  const [selectedPeriod, setSelectedPeriod] = useState('');

  const { data: periods = [] } = useQuery({
    queryKey: ['pay-periods'],
    queryFn: fetchPayPeriods,
  });

  const zipMutation = useMutation({
    mutationFn: (periodId: string) => exportSignedPdfsZip(periodId),
    onSuccess: (blob) => {
      const period = periods.find((p) => p.id === selectedPeriod);
      const label = period?.label ?? 'periodo';
      downloadBlob(blob, `holerites_${label.replace(/\s/g, '_')}_assinados.zip`);
    },
    onError: () => alert('Nenhum documento assinado encontrado para este período.'),
  });

  const csvEmployeesMutation = useMutation({
    mutationFn: () => exportEmployeesCsv(),
    onSuccess: (blob) => downloadBlob(blob, 'funcionarios.csv'),
  });

  const csvAuditMutation = useMutation({
    mutationFn: () => exportAuditLogsCsv(),
    onSuccess: (blob) => downloadBlob(blob, 'audit_logs.csv'),
  });

  const fullExportMutation = useMutation({
    mutationFn: () => exportAllData(),
    onSuccess: (blob) => downloadBlob(blob, `holeritesign_backup_completo_${new Date().toISOString().slice(0,10)}.zip`),
    onError: () => alert('Erro ao gerar backup completo.'),
  });

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Backup & Exportação</h1>
        <p className="mt-1 text-sm text-gray-500">Exporte dados do sistema para backup ou análise.</p>
      </div>

      {/* Full Export */}
      <div className="bg-white rounded-lg shadow p-6 border-2 border-primary-200">
        <h2 className="text-lg font-semibold text-gray-900 mb-2">Backup Completo (ZIP)</h2>
        <p className="text-sm text-gray-500 mb-4">
          Exporte todos os dados: funcionários, auditoria e todos os PDFs organizados por período.
        </p>
        <button
          onClick={() => fullExportMutation.mutate()}
          disabled={fullExportMutation.isPending}
          className="bg-primary-600 text-white px-6 py-2 rounded-md text-sm font-medium hover:bg-primary-700 disabled:opacity-50 flex items-center gap-2"
        >
          {fullExportMutation.isPending ? (
            <>
              <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" />
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
              </svg>
              Gerando backup...
            </>
          ) : (
            <>
              <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
              </svg>
              Baixar Backup Completo
            </>
          )}
        </button>
      </div>

      {/* Signed PDFs ZIP (BAK-01) */}
      <div className="bg-white rounded-lg shadow p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-2">Holerites Assinados (ZIP)</h2>
        <p className="text-sm text-gray-500 mb-4">
          Baixe todos os holerites assinados de um período como arquivo ZIP.
        </p>
        <div className="flex items-end gap-4">
          <div className="flex-1">
            <label htmlFor="period-select" className="block text-sm font-medium text-gray-700 mb-1">Período</label>
            <select
              id="period-select"
              value={selectedPeriod}
              onChange={(e) => setSelectedPeriod(e.target.value)}
              className="w-full border border-gray-300 rounded-md px-3 py-2 text-sm focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="">Selecione um período...</option>
              {periods.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.label} ({p.documentCount} documento{p.documentCount !== 1 ? 's' : ''})
                </option>
              ))}
            </select>
          </div>
          <button
            onClick={() => selectedPeriod && zipMutation.mutate(selectedPeriod)}
            disabled={!selectedPeriod || zipMutation.isPending}
            className="bg-blue-600 text-white px-6 py-2 rounded-md text-sm font-medium hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
          >
            {zipMutation.isPending ? (
              <>
                <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" />
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                </svg>
                Gerando...
              </>
            ) : (
              <>
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                </svg>
                Baixar ZIP
              </>
            )}
          </button>
        </div>
      </div>

      {/* Quick Exports */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">

        {/* Employee CSV (BAK-03) */}
        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-2">Lista de Funcionários (CSV)</h2>
          <p className="text-sm text-gray-500 mb-4">
            Exporte a lista de todos os funcionários cadastrados com nome, contato e status.
          </p>
          <button
            onClick={() => csvEmployeesMutation.mutate()}
            disabled={csvEmployeesMutation.isPending}
            className="bg-green-600 text-white px-6 py-2 rounded-md text-sm font-medium hover:bg-green-700 disabled:opacity-50 flex items-center gap-2"
          >
            {csvEmployeesMutation.isPending ? (
              <>
                <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" />
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                </svg>
                Exportando...
              </>
            ) : (
              <>
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                </svg>
                Baixar CSV
              </>
            )}
          </button>
        </div>

        {/* Audit Logs CSV (BAK-04) */}
        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-2">Logs de Auditoria (CSV)</h2>
          <p className="text-sm text-gray-500 mb-4">
            Exporte os logs de auditoria com todos os eventos, IPs e timestamps.
          </p>
          <button
            onClick={() => csvAuditMutation.mutate()}
            disabled={csvAuditMutation.isPending}
            className="bg-purple-600 text-white px-6 py-2 rounded-md text-sm font-medium hover:bg-purple-700 disabled:opacity-50 flex items-center gap-2"
          >
            {csvAuditMutation.isPending ? (
              <>
                <svg className="animate-spin h-4 w-4" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" fill="none" />
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                </svg>
                Exportando...
              </>
            ) : (
              <>
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
                </svg>
                Baixar CSV
              </>
            )}
          </button>
        </div>
      </div>
    </div>
  );
}
