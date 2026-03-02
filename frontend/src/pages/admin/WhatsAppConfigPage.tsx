import { useState, useEffect } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { fetchWhatsAppStatus, fetchWhatsAppQrCode, createWhatsAppInstance, logoutWhatsApp } from '../../services/api';

export default function WhatsAppConfigPage() {
  const queryClient = useQueryClient();
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [polling, setPolling] = useState(false);
  const [createQr, setCreateQr] = useState<{ base64: string; pairingCode?: string | null } | null>(null);
  const [debugInfo, setDebugInfo] = useState<string | null>(null);

  // Fetch connection status
  const { data: status, isLoading: statusLoading } = useQuery({
    queryKey: ['whatsapp-status'],
    queryFn: fetchWhatsAppStatus,
    refetchInterval: polling ? 5000 : 30000,
  });

  const isConnected = status?.state === 'open';

  // Stop polling when connected
  useEffect(() => {
    if (isConnected) {
      setPolling(false);
      setCreateQr(null);
    }
  }, [isConnected]);

  // Create instance mutation
  const createMutation = useMutation({
    mutationFn: createWhatsAppInstance,
    onSuccess: (data: any) => {
      setSuccess('Instância criada com sucesso!');
      setError('');
      setPolling(true);

      // Debug: show raw response
      if (data?.rawBody) {
        setDebugInfo(data.rawBody);
      }

      // Capture QR code from create response (multiple possible locations)
      const qrBase64 = data?.qrcode?.base64;
      if (qrBase64) {
        setCreateQr({ base64: qrBase64, pairingCode: data?.qrcode?.pairingCode });
      }

      queryClient.invalidateQueries({ queryKey: ['whatsapp-status'] });
      queryClient.invalidateQueries({ queryKey: ['whatsapp-qr'] });
    },
    onError: (err: any) => {
      const msg = err.response?.data?.message || err.message || 'Erro ao criar instância';
      const rawBody = err.response?.data?.rawBody;
      setError(`Falha ao criar instância WhatsApp: ${msg}`);
      if (rawBody) setDebugInfo(rawBody);
      setSuccess('');
    },
  });

  // QR code query (poll when not connected)
  const { data: qrData, isLoading: qrLoading, refetch: refetchQr } = useQuery({
    queryKey: ['whatsapp-qr'],
    queryFn: fetchWhatsAppQrCode,
    enabled: !isConnected && polling,
    refetchInterval: !isConnected && polling ? 5000 : false,
    retry: 5,
    retryDelay: 2000,
  });

  // Effective QR: prefer polled qrData, fallback to create response QR
  const qrBase64 = qrData?.base64 || createQr?.base64 || null;
  const qrPairingCode = qrData?.pairingCode || createQr?.pairingCode || null;

  // Logout mutation
  const logoutMutation = useMutation({
    mutationFn: logoutWhatsApp,
    onSuccess: () => {
      setSuccess('WhatsApp desconectado.');
      setError('');
      setCreateQr(null);
      setDebugInfo(null);
      queryClient.invalidateQueries({ queryKey: ['whatsapp-status'] });
    },
    onError: (err: any) => {
      setError(err.response?.data?.message || 'Erro ao desconectar');
    },
  });

  const handleConnect = () => {
    setError('');
    setSuccess('');
    setCreateQr(null);
    setDebugInfo(null);
    createMutation.mutate();
  };

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Configuração WhatsApp</h1>
        <p className="text-sm text-gray-500 mt-1">
          Conecte seu WhatsApp para enviar notificações de holerites diretamente para os funcionários.
        </p>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg text-sm">
          {error}
        </div>
      )}
      {success && (
        <div className="bg-green-50 border border-green-200 text-green-700 px-4 py-3 rounded-lg text-sm">
          {success}
        </div>
      )}

      {/* Connection Status Card */}
      <div className="bg-white rounded-lg shadow p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Status da Conexão</h2>

        {statusLoading ? (
          <div className="flex items-center gap-2 text-gray-500">
            <div className="animate-spin h-4 w-4 border-2 border-gray-300 border-t-blue-600 rounded-full" />
            Verificando...
          </div>
        ) : (
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-3">
              <div className={`w-3 h-3 rounded-full ${isConnected ? 'bg-green-500' : 'bg-red-400'}`} />
              <div>
                <p className="font-medium text-gray-900">
                  {isConnected ? 'Conectado' : 'Desconectado'}
                </p>
                <p className="text-sm text-gray-500">
                  {status?.instance?.instanceName
                    ? `Instância: ${status.instance.instanceName}`
                    : 'Nenhuma instância ativa'}
                </p>
              </div>
            </div>

            {isConnected ? (
              <button
                onClick={() => logoutMutation.mutate()}
                disabled={logoutMutation.isPending}
                className="px-4 py-2 text-sm font-medium text-red-700 bg-red-50 hover:bg-red-100 rounded-lg"
              >
                {logoutMutation.isPending ? 'Desconectando...' : 'Desconectar'}
              </button>
            ) : (
              <button
                onClick={handleConnect}
                disabled={createMutation.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-green-600 hover:bg-green-700 rounded-lg disabled:opacity-50"
              >
                {createMutation.isPending ? (
                  <span className="flex items-center gap-2">
                    <span className="animate-spin h-4 w-4 border-2 border-white border-t-transparent rounded-full" />
                    Aguardando QR Code...
                  </span>
                ) : 'Conectar WhatsApp'}
              </button>
            )}
          </div>
        )}
      </div>

      {/* QR Code Card - show when not connected and polling or has QR */}
      {!isConnected && (polling || qrBase64) && (
        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">Escaneie o QR Code</h2>
          <p className="text-sm text-gray-500 mb-4">
            Abra o WhatsApp no seu celular → Menu (⋮) → Dispositivos conectados → Conectar dispositivo → Escaneie o QR code abaixo.
          </p>

          {qrLoading && !qrBase64 ? (
            <div className="flex items-center justify-center h-64">
              <div className="animate-spin h-8 w-8 border-4 border-gray-300 border-t-green-600 rounded-full" />
            </div>
          ) : qrBase64 ? (
            <div className="flex flex-col items-center gap-4">
              <img
                src={qrBase64.startsWith('data:') ? qrBase64 : `data:image/png;base64,${qrBase64}`}
                alt="WhatsApp QR Code"
                className="w-64 h-64 border rounded-lg"
              />
              {qrPairingCode && (
                <div className="text-center">
                  <p className="text-sm text-gray-500">Ou use o código de pareamento:</p>
                  <p className="text-2xl font-mono font-bold text-gray-900 tracking-widest mt-1">
                    {qrPairingCode}
                  </p>
                </div>
              )}
              <button
                onClick={() => refetchQr()}
                className="text-sm text-blue-600 hover:text-blue-800"
              >
                Atualizar QR Code
              </button>
            </div>
          ) : (
            <div className="text-center py-8">
              <p className="text-gray-500">QR Code não disponível. Aguarde ou clique abaixo.</p>
              <button
                onClick={() => refetchQr()}
                className="mt-2 text-sm text-blue-600 hover:text-blue-800"
              >
                Tentar novamente
              </button>
            </div>
          )}
        </div>
      )}

      {/* Debug Info (temporary) */}
      {debugInfo && (
        <div className="bg-gray-50 rounded-lg p-4">
          <h3 className="font-semibold text-gray-700 mb-2 text-sm">Debug - Resposta Evolution API:</h3>
          <pre className="text-xs text-gray-600 overflow-x-auto whitespace-pre-wrap break-all bg-gray-100 p-3 rounded">
            {debugInfo}
          </pre>
        </div>
      )}

      {/* Instructions */}
      <div className="bg-blue-50 rounded-lg p-6">
        <h3 className="font-semibold text-blue-900 mb-2">Como funciona?</h3>
        <ol className="list-decimal list-inside space-y-2 text-sm text-blue-800">
          <li>Clique em <strong>Conectar WhatsApp</strong> para criar a instância</li>
          <li>Escaneie o QR Code com o WhatsApp do celular</li>
          <li>Quando conectado, as notificações poderão ser enviadas via WhatsApp</li>
          <li>Na tela de Documentos, clique em <strong>Enviar ▾</strong> e escolha <strong>WhatsApp</strong></li>
        </ol>

        <div className="mt-4 p-3 bg-yellow-50 border border-yellow-200 rounded-lg">
          <p className="text-xs text-yellow-800">
            <strong>Importante:</strong> O número de WhatsApp conectado será usado para enviar as notificações.
            O funcionário precisa ter um número de WhatsApp cadastrado no sistema.
          </p>
        </div>
      </div>
    </div>
  );
}
