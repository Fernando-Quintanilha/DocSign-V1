import { useState, useRef } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { fetchDocuments, fetchEmployees, fetchPayPeriods, uploadDocument, deleteDocument, downloadDocument, generateSigningToken, sendNotification, replaceDocument } from '../../services/api';
import type { Document, Employee, PayPeriod } from '../../types';

const statusLabels: Record<string, { label: string; color: string }> = {
  Uploaded: { label: 'Enviado', color: 'bg-blue-100 text-blue-800' },
  Sent: { label: 'Notificado', color: 'bg-yellow-100 text-yellow-800' },
  Signed: { label: 'Assinado', color: 'bg-green-100 text-green-800' },
  Expired: { label: 'Expirado', color: 'bg-red-100 text-red-800' },
};

export default function DocumentsPage() {
  const queryClient = useQueryClient();
  const [showUpload, setShowUpload] = useState(false);
  const [selectedEmployee, setSelectedEmployee] = useState('');
  const [year, setYear] = useState(new Date().getFullYear());
  const [month, setMonth] = useState(new Date().getMonth() + 1);
  const [file, setFile] = useState<File | null>(null);
  const [error, setError] = useState('');
  const [signingUrl, setSigningUrl] = useState('');
  const [copyMsg, setCopyMsg] = useState('');
  // Filters
  const [filterEmployee, setFilterEmployee] = useState('');
  const [filterPeriod, setFilterPeriod] = useState('');
  const [filterStatus, setFilterStatus] = useState('');

  const { data: documents = [], isLoading } = useQuery({
    queryKey: ['documents', filterEmployee, filterPeriod],
    queryFn: () => fetchDocuments({
      employeeId: filterEmployee || undefined,
      payPeriodId: filterPeriod || undefined,
    }),
  });

  const { data: employees = [] } = useQuery({
    queryKey: ['employees'],
    queryFn: () => fetchEmployees(),
  });

  const { data: payPeriods = [] } = useQuery({
    queryKey: ['pay-periods'],
    queryFn: fetchPayPeriods,
  });

  const uploadMutation = useMutation({
    mutationFn: () => {
      if (!file || !selectedEmployee) throw new Error('Selecione funcionário e arquivo');
      return uploadDocument(file, selectedEmployee, year, month);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['documents'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard-enhanced'] });
      setShowUpload(false);
      setFile(null);
      setSelectedEmployee('');
      setError('');
    },
    onError: (err: any) => {
      setError(err.response?.data?.message || err.message || 'Erro ao enviar documento');
    },
  });

  const deleteMutation = useMutation({
    mutationFn: deleteDocument,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['documents'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard-enhanced'] });
    },
  });

  const generateTokenMutation = useMutation({
    mutationFn: (docId: string) => generateSigningToken(docId),
    onSuccess: (data) => {
      setSigningUrl(data.signingUrl);
      setCopyMsg('');
      queryClient.invalidateQueries({ queryKey: ['documents'] });
    },
    onError: (err: any) => {
      setError(err.response?.data?.message || 'Erro ao gerar link');
    },
  });

  const sendNotifMutation = useMutation({
    mutationFn: ({ docId, channel }: { docId: string; channel: string }) =>
      sendNotification(docId, channel),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['documents'] });
    },
    onError: (err: any) => {
      setError(err.response?.data?.message || 'Erro ao enviar notificação');
    },
  });

  const replaceInputRef = useRef<HTMLInputElement>(null);
  const [replacingDocId, setReplacingDocId] = useState('');

  const replaceMutation = useMutation({
    mutationFn: ({ docId, file }: { docId: string; file: File }) => replaceDocument(docId, file),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['documents'] });
      setReplacingDocId('');
    },
    onError: (err: any) => {
      setError(err.response?.data?.message || 'Erro ao substituir documento');
    },
  });

  const handleReplace = (docId: string) => {
    setReplacingDocId(docId);
    setTimeout(() => replaceInputRef.current?.click(), 0);
  };

  const handleReplaceFile = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file && replacingDocId) {
      replaceMutation.mutate({ docId: replacingDocId, file });
    }
    if (replaceInputRef.current) replaceInputRef.current.value = '';
  };

  const handleCopyLink = () => {
    navigator.clipboard.writeText(signingUrl);
    setCopyMsg('Link copiado!');
    setTimeout(() => setCopyMsg(''), 3000);
  };

  const handleDownload = async (docId: string, filename: string, type: 'original' | 'signed') => {
    try {
      const blob = await downloadDocument(docId, type);
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = filename;
      a.click();
      URL.revokeObjectURL(url);
    } catch {
      setError('Erro ao baixar documento.');
    }
  };

  const handleUpload = (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    uploadMutation.mutate();
  };

  const formatDate = (dateStr: string) =>
    new Date(dateStr).toLocaleDateString('pt-BR');

  const formatSize = (bytes: number) => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  };

  return (
    <div>
      {/* Hidden replace file input */}
      <input
        ref={replaceInputRef}
        type="file"
        accept=".pdf,application/pdf"
        className="hidden"
        aria-label="Selecionar PDF para substituição"
        onChange={handleReplaceFile}
      />

      <div className="flex items-center justify-between flex-wrap gap-2">
        <h1 className="text-2xl font-bold text-gray-900">Documentos</h1>
        <button
          onClick={() => setShowUpload(!showUpload)}
          className="bg-primary-600 text-white px-4 py-2 rounded-md text-sm font-medium hover:bg-primary-700"
        >
          {showUpload ? 'Cancelar' : '+ Upload Holerite'}
        </button>
      </div>

      {/* Upload Form */}
      {showUpload && (
        <form onSubmit={handleUpload} className="mt-4 bg-white shadow rounded-lg p-6 space-y-4">
          {error && <div className="bg-red-50 text-red-700 p-3 rounded-md text-sm">{error}</div>}

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
              <label htmlFor="upload-employee" className="block text-sm font-medium text-gray-700">Funcionário *</label>
              <select
                id="upload-employee"
                required value={selectedEmployee}
                onChange={(e) => setSelectedEmployee(e.target.value)}
                className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
              >
                <option value="">Selecione...</option>
                {employees.map((emp: Employee) => (
                  <option key={emp.id} value={emp.id}>{emp.name}</option>
                ))}
              </select>
            </div>
            <div>
              <label htmlFor="upload-year" className="block text-sm font-medium text-gray-700">Ano *</label>
              <input
                id="upload-year"
                type="number" required min={2020} max={2100} value={year}
                onChange={(e) => setYear(parseInt(e.target.value))}
                className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
              />
            </div>
            <div>
              <label htmlFor="upload-month" className="block text-sm font-medium text-gray-700">Mês *</label>
              <select
                id="upload-month"
                required value={month}
                onChange={(e) => setMonth(parseInt(e.target.value))}
                className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
              >
                {['Janeiro','Fevereiro','Março','Abril','Maio','Junho','Julho','Agosto','Setembro','Outubro','Novembro','Dezembro']
                  .map((m, i) => <option key={i+1} value={i+1}>{m}</option>)}
              </select>
            </div>
          </div>

          <div>
            <label htmlFor="upload-file" className="block text-sm font-medium text-gray-700">Arquivo PDF *</label>
            <input
              id="upload-file"
              type="file" required accept=".pdf,application/pdf"
              onChange={(e) => setFile(e.target.files?.[0] || null)}
              className="mt-1 block w-full text-sm text-gray-500 file:mr-4 file:py-2 file:px-4 file:rounded-md file:border-0 file:text-sm file:font-medium file:bg-primary-50 file:text-primary-700 hover:file:bg-primary-100"
            />
          </div>

          <button
            type="submit" disabled={uploadMutation.isPending}
            className="bg-primary-600 text-white px-6 py-2 rounded-md text-sm font-medium hover:bg-primary-700 disabled:opacity-50"
          >
            {uploadMutation.isPending ? 'Enviando...' : 'Enviar'}
          </button>
        </form>
      )}

      {/* Signing URL Banner */}
      {signingUrl && (
        <div className="mt-4 bg-green-50 border border-green-200 rounded-lg p-4">
          <p className="text-sm font-medium text-green-800 mb-2">Link de Assinatura gerado:</p>
          <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-2">
            <input
              type="text" readOnly value={signingUrl}
              aria-label="Link de assinatura"
              className="flex-1 min-w-0 bg-white rounded-md border border-green-300 px-3 py-2 text-sm font-mono truncate"
            />
            <div className="flex gap-2 shrink-0">
              <button
                onClick={handleCopyLink}
                className="bg-green-600 text-white px-4 py-2 rounded-md text-sm font-medium hover:bg-green-700"
              >
                Copiar
              </button>
              <button
                onClick={() => setSigningUrl('')}
                className="text-green-600 hover:text-green-800 text-sm font-medium"
              >
                Fechar
              </button>
            </div>
          </div>
          {copyMsg && <p className="mt-1 text-xs text-green-600">{copyMsg}</p>}
        </div>
      )}

      {/* Filters */}
      <div className="mt-4 flex flex-wrap gap-3">
        <select
          aria-label="Filtrar por funcionário"
          value={filterEmployee}
          onChange={(e) => setFilterEmployee(e.target.value)}
          className="rounded-md border border-gray-300 px-3 py-2 text-sm shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
        >
          <option value="">Todos os Funcionários</option>
          {employees.map((emp: Employee) => (
            <option key={emp.id} value={emp.id}>{emp.name}</option>
          ))}
        </select>
        <select
          aria-label="Filtrar por período"
          value={filterPeriod}
          onChange={(e) => setFilterPeriod(e.target.value)}
          className="rounded-md border border-gray-300 px-3 py-2 text-sm shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
        >
          <option value="">Todos os Períodos</option>
          {payPeriods.map((p: PayPeriod) => (
            <option key={p.id} value={p.id}>{p.label}</option>
          ))}
        </select>
        <select
          aria-label="Filtrar por status"
          value={filterStatus}
          onChange={(e) => setFilterStatus(e.target.value)}
          className="rounded-md border border-gray-300 px-3 py-2 text-sm shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
        >
          <option value="">Todos os Status</option>
          <option value="Uploaded">Enviado</option>
          <option value="Sent">Notificado</option>
          <option value="Signed">Assinado</option>
          <option value="Expired">Expirado</option>
        </select>
        {(filterEmployee || filterPeriod || filterStatus) && (
          <button
            onClick={() => { setFilterEmployee(''); setFilterPeriod(''); setFilterStatus(''); }}
            className="text-sm text-gray-500 hover:text-gray-700 underline"
          >
            Limpar filtros
          </button>
        )}
      </div>

      {/* Table */}
      <div className="mt-6 bg-white shadow rounded-lg overflow-x-auto">
        <table className="min-w-[900px] w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Funcionário</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Período</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Arquivo</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Data</th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Ações</th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {isLoading ? (
              <tr><td colSpan={6} className="px-6 py-12 text-center text-gray-500">Carregando...</td></tr>
            ) : documents.length === 0 ? (
              <tr><td colSpan={6} className="px-6 py-12 text-center text-gray-500">Nenhum documento encontrado.</td></tr>
            ) : (
              documents
                .filter((doc: Document) => !filterStatus || doc.status === filterStatus)
                .map((doc: Document) => {
                const st = statusLabels[doc.status] || { label: doc.status, color: 'bg-gray-100 text-gray-800' };
                return (
                  <tr key={doc.id} className="hover:bg-gray-50">
                    <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">{doc.employeeName}</td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">{doc.payPeriodLabel}</td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      <span title={doc.originalFilename}>{doc.originalFilename.length > 25 ? doc.originalFilename.slice(0, 25) + '...' : doc.originalFilename}</span>
                      <span className="ml-2 text-xs text-gray-400">{formatSize(doc.fileSizeBytes)}</span>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${st.color}`}>
                        {st.label}
                      </span>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {formatDate(doc.createdAt)}
                      {doc.signedAt && <span className="block text-xs text-green-600">Assinado: {formatDate(doc.signedAt)}</span>}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm">
                      <div className="flex items-center justify-end gap-2 flex-wrap">
                      {doc.status === 'Signed' ? (
                        <>
                          <button
                            onClick={() => handleDownload(doc.id, `assinado_${doc.originalFilename}`, 'signed')}
                            className="inline-flex items-center gap-1 bg-green-600 text-white px-3 py-1.5 rounded-md text-xs font-medium hover:bg-green-700"
                            title="Baixar PDF assinado com comprovante"
                          >
                            ⬇ Assinado
                          </button>
                          <button
                            onClick={() => handleDownload(doc.id, doc.originalFilename, 'original')}
                            className="text-gray-400 hover:text-gray-600 text-xs underline"
                            title="Baixar original sem assinatura"
                          >
                            Original
                          </button>
                        </>
                      ) : (
                        <button
                          onClick={() => handleDownload(doc.id, doc.originalFilename, 'original')}
                          className="inline-flex items-center gap-1 text-gray-600 hover:text-gray-800 text-xs font-medium"
                          title="Baixar PDF"
                        >
                          ⬇ Baixar
                        </button>
                      )}
                      {doc.status !== 'Signed' && (
                        <>
                          <button
                            onClick={() => generateTokenMutation.mutate(doc.id)}
                            disabled={generateTokenMutation.isPending}
                            className="text-primary-600 hover:text-primary-800 font-medium"
                          >
                            Gerar Link
                          </button>
                          {doc.status === 'Uploaded' && (
                            <>
                              <button
                                onClick={() => sendNotifMutation.mutate({ docId: doc.id, channel: 'Email' })}
                                disabled={sendNotifMutation.isPending}
                                className="inline-flex items-center gap-1 text-indigo-600 hover:text-indigo-800 font-medium"
                                title="Enviar por E-mail"
                              >
                                📧 E-mail
                              </button>
                              <button
                                onClick={() => sendNotifMutation.mutate({ docId: doc.id, channel: 'WhatsApp' })}
                                disabled={sendNotifMutation.isPending}
                                className="inline-flex items-center gap-1 text-green-600 hover:text-green-800 font-medium"
                                title="Enviar por WhatsApp"
                              >
                                💬 WhatsApp
                              </button>
                            </>
                          )}
                        </>
                      )}
                      {doc.status === 'Uploaded' && (
                        <button
                          onClick={() => handleReplace(doc.id)}
                          disabled={replaceMutation.isPending}
                          className="text-orange-600 hover:text-orange-800 font-medium"
                        >
                          Substituir
                        </button>
                      )}
                      {doc.status !== 'Signed' && (
                        <button
                          onClick={() => { if (confirm(doc.status === 'Sent' ? 'Este documento já foi enviado ao funcionário. O link de assinatura será invalidado. Deseja continuar?' : 'Excluir documento?')) deleteMutation.mutate(doc.id); }}
                          className="text-red-600 hover:text-red-800 font-medium"
                        >
                          Excluir
                        </button>
                      )}
                      </div>
                    </td>
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
