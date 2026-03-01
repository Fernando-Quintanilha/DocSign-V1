import { useState, useRef, useCallback, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { validateSigningToken, verifyIdentity, getSigningDocument, signDocument } from '../../services/api';
import type { ValidateTokenResponse, SigningDocument } from '../../types';

type Step = 'loading' | 'invalid' | 'verify' | 'view' | 'selfie' | 'signing' | 'done';

export default function SignPage() {
  const { token } = useParams<{ token: string }>();
  const [step, setStep] = useState<Step>('loading');
  const [error, setError] = useState('');
  const [tokenInfo, setTokenInfo] = useState<ValidateTokenResponse | null>(null);
  const [document, setDocument] = useState<SigningDocument | null>(null);

  // Identity verification
  const [cpf, setCpf] = useState('');
  const [birthDate, setBirthDate] = useState('');

  // Selfie
  const videoRef = useRef<HTMLVideoElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [photoBase64, setPhotoBase64] = useState('');
  const [photoPreview, setPhotoPreview] = useState('');
  const [cameraReady, setCameraReady] = useState(false);
  const [cameraFailed, setCameraFailed] = useState(false);
  const [consent, setConsent] = useState(false);

  // Minimum viewing time (30 seconds)
  const MIN_VIEW_SECONDS = 30;
  const [viewSeconds, setViewSeconds] = useState(0);
  const viewTimerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  // Result
  const [signedAt, setSignedAt] = useState('');

  // ─── Step 1: Validate token on mount ───
  useEffect(() => {
    if (!token) {
      setStep('invalid');
      return;
    }
    validateSigningToken(token)
      .then((info) => {
        if (!info.valid) {
          setStep('invalid');
          return;
        }
        setTokenInfo(info);
        // If no CPF/DOB required, skip verification
        if (!info.requiresCpf && !info.requiresBirthDate) {
          loadDocument();
        } else {
          setStep('verify');
        }
      })
      .catch(() => setStep('invalid'));
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);

  // ─── Load document after verification ───
  const loadDocument = useCallback(async () => {
    if (!token) return;
    try {
      const doc = await getSigningDocument(token);
      setDocument(doc);
      setStep('view');
      // Start viewing timer
      setViewSeconds(0);
      viewTimerRef.current = setInterval(() => {
        setViewSeconds((prev) => {
          const next = prev + 1;
          if (next >= MIN_VIEW_SECONDS) {
            if (viewTimerRef.current) clearInterval(viewTimerRef.current);
          }
          return next;
        });
      }, 1000);
    } catch {
      setError('Não foi possível carregar o documento.');
      setStep('invalid');
    }
  }, [token]);

  // Cleanup timer on unmount
  useEffect(() => {
    return () => {
      if (viewTimerRef.current) clearInterval(viewTimerRef.current);
    };
  }, []);

  // ─── Step 2: Verify identity ───
  const handleVerify = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    if (!token) return;

    try {
      const result = await verifyIdentity(token, {
        cpf: cpf || undefined,
        birthDate: birthDate || undefined,
      });
      if (result.verified) {
        await loadDocument();
      } else {
        setError(result.message || 'Verificação falhou.');
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Erro na verificação.');
    }
  };

  // ─── Step 3: Start camera for selfie ───
  const startCamera = async () => {
    setStep('selfie');
    try {
      const stream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: 'user', width: { ideal: 640 }, height: { ideal: 480 } },
      });
      if (videoRef.current) {
        videoRef.current.srcObject = stream;
        videoRef.current.play();
        setCameraReady(true);
      }
    } catch {
      setCameraFailed(true);
      setError('Não foi possível acessar a câmera. Você pode enviar uma foto da galeria.');
    }
  };

  const capturePhoto = () => {
    if (!videoRef.current || !canvasRef.current) return;
    const video = videoRef.current;
    const canvas = canvasRef.current;
    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    const ctx = canvas.getContext('2d')!;
    ctx.drawImage(video, 0, 0);
    const dataUrl = canvas.toDataURL('image/jpeg', 0.85);
    setPhotoPreview(dataUrl);
    setPhotoBase64(dataUrl.split(',')[1]);

    // Stop camera
    const stream = video.srcObject as MediaStream;
    stream?.getTracks().forEach((t) => t.stop());
    setCameraReady(false);
  };

  const retakePhoto = () => {
    setPhotoBase64('');
    setPhotoPreview('');
    setCameraFailed(false);
    startCamera();
  };

  // ─── Fallback: gallery/file upload ───
  const handleFileUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    if (!file.type.startsWith('image/')) {
      setError('Selecione um arquivo de imagem (JPG ou PNG).');
      return;
    }
    if (file.size > 5 * 1024 * 1024) {
      setError('A imagem deve ter no máximo 5 MB.');
      return;
    }
    setError('');
    const reader = new FileReader();
    reader.onload = () => {
      const dataUrl = reader.result as string;
      setPhotoPreview(dataUrl);
      setPhotoBase64(dataUrl.split(',')[1]);
    };
    reader.readAsDataURL(file);
  };

  // ─── Step 4: Sign document ───
  const handleSign = async () => {
    if (!token || !photoBase64 || !consent) return;
    setStep('signing');
    setError('');

    try {
      // Get geolocation if available
      let geo: string | undefined;
      try {
        const pos = await new Promise<GeolocationPosition>((resolve, reject) =>
          navigator.geolocation.getCurrentPosition(resolve, reject, { timeout: 5000 })
        );
        geo = JSON.stringify({
          lat: pos.coords.latitude,
          lng: pos.coords.longitude,
          accuracy: pos.coords.accuracy,
        });
      } catch {
        // Geolocation not available — continue without
      }

      const result = await signDocument(token, {
        photoBase64,
        photoMimeType: 'image/jpeg',
        consentGiven: true,
        geolocation: geo,
      });

      if (result.success) {
        setSignedAt(result.signedAt);
        setStep('done');
      } else {
        setError(result.message);
        setStep('selfie');
      }
    } catch (err: any) {
      setError(err.response?.data?.message || 'Erro ao assinar.');
      setStep('selfie');
    }
  };

  // ─── Render ───
  if (step === 'loading') {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="animate-pulse text-gray-500 text-lg">Carregando...</div>
      </div>
    );
  }

  if (step === 'invalid') {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
        <div className="max-w-md w-full bg-white rounded-lg shadow p-8 text-center">
          <div className="text-red-500 text-5xl mb-4">&#10005;</div>
          <h1 className="text-xl font-bold text-gray-900">Link Inválido ou Expirado</h1>
          <p className="mt-2 text-sm text-gray-600">
            Este link de assinatura não é mais válido. Solicite um novo link ao seu departamento pessoal.
          </p>
          {error && <p className="mt-2 text-sm text-red-600">{error}</p>}
        </div>
      </div>
    );
  }

  if (step === 'done') {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
        <div className="max-w-md w-full bg-white rounded-lg shadow p-8 text-center">
          <div className="text-green-500 text-5xl mb-4">&#10003;</div>
          <h1 className="text-xl font-bold text-gray-900">Assinatura Concluída!</h1>
          <p className="mt-2 text-sm text-gray-600">
            Seu holerite foi assinado com sucesso em{' '}
            {new Date(signedAt).toLocaleString('pt-BR')}.
          </p>
          <p className="mt-4 text-xs text-gray-400">
            Uma cópia foi registrada com sua selfie, IP e data/hora para comprovação legal.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 py-8 px-4">
      <div className="max-w-2xl mx-auto">
        {/* Header */}
        <div className="bg-white rounded-lg shadow p-6 mb-6">
          <h1 className="text-2xl font-bold text-gray-900 text-center">
            Assinatura de Holerite
          </h1>
          {tokenInfo && (
            <div className="mt-3 text-center">
              <p className="text-sm text-gray-600">
                <span className="font-medium">{tokenInfo.employeeName}</span>
                {' — '}
                <span className="text-primary-600">{tokenInfo.companyName}</span>
              </p>
              <p className="text-xs text-gray-400 mt-1">{tokenInfo.payPeriodLabel}</p>
            </div>
          )}
        </div>

        {error && (
          <div className="bg-red-50 text-red-700 p-4 rounded-lg mb-6 text-sm">{error}</div>
        )}

        {/* Step: Verify Identity */}
        {step === 'verify' && (
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">
              1. Verificação de Identidade
            </h2>
            <p className="text-sm text-gray-600 mb-4">
              Para sua segurança, confirme seus dados antes de acessar o documento.
            </p>
            <form onSubmit={handleVerify} className="space-y-4">
              {tokenInfo?.requiresCpf && (
                <div>
                  <label htmlFor="sign-cpf" className="block text-sm font-medium text-gray-700">CPF</label>
                  <input
                    id="sign-cpf"
                    type="text"
                    required
                    placeholder="000.000.000-00"
                    value={cpf}
                    onChange={(e) => setCpf(e.target.value)}
                    className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
                  />
                </div>
              )}
              {tokenInfo?.requiresBirthDate && (
                <div>
                  <label htmlFor="sign-birthdate" className="block text-sm font-medium text-gray-700">
                    Data de Nascimento
                  </label>
                  <input
                    id="sign-birthdate"
                    type="date"
                    required
                    value={birthDate}
                    onChange={(e) => setBirthDate(e.target.value)}
                    className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
                  />
                </div>
              )}
              <button
                type="submit"
                className="w-full bg-primary-600 text-white py-2 px-4 rounded-md text-sm font-medium hover:bg-primary-700"
              >
                Verificar Identidade
              </button>
            </form>
          </div>
        )}

        {/* Step: View Document */}
        {step === 'view' && document && (
          <div className="space-y-6">
            <div className="bg-white rounded-lg shadow p-6">
              <h2 className="text-lg font-semibold text-gray-900 mb-4">
                2. Visualize seu Holerite
              </h2>
              <div className="border border-gray-200 rounded-lg overflow-hidden">
                <iframe
                  src={`/api${document.downloadUrl}`}
                  className="w-full"
                  style={{ height: '600px' }}
                  title="Holerite PDF"
                />
              </div>
              <div className="mt-3 flex items-center justify-between text-sm text-gray-500">
                <span>{document.originalFilename}</span>
                <span>{(document.fileSizeBytes / 1024).toFixed(1)} KB</span>
              </div>
            </div>

            <div className="bg-white rounded-lg shadow p-6">
              <h2 className="text-lg font-semibold text-gray-900 mb-4">
                3. Assinar com Selfie
              </h2>
              <p className="text-sm text-gray-600 mb-4">
                Após verificar o conteúdo do holerite, tire uma selfie para confirmar a assinatura.
                Sua foto será armazenada como prova legal da assinatura.
              </p>
              {viewSeconds < MIN_VIEW_SECONDS ? (
                <div>
                  <div className="w-full bg-gray-200 rounded-full h-2.5 mb-3">
                    <div
                      className="bg-primary-600 h-2.5 rounded-full transition-all duration-1000"
                      style={{ width: `${(viewSeconds / MIN_VIEW_SECONDS) * 100}%` }}
                    />
                  </div>
                  <button
                    disabled
                    className="w-full bg-gray-400 text-white py-3 px-4 rounded-md font-medium cursor-not-allowed"
                  >
                    Leia o documento ({MIN_VIEW_SECONDS - viewSeconds}s restantes)
                  </button>
                </div>
              ) : (
                <button
                  onClick={startCamera}
                  className="w-full bg-primary-600 text-white py-3 px-4 rounded-md font-medium hover:bg-primary-700"
                >
                  Abrir Câmera e Assinar
                </button>
              )}
            </div>
          </div>
        )}

        {/* Step: Selfie Capture */}
        {(step === 'selfie' || step === 'signing') && (
          <div className="bg-white rounded-lg shadow p-6">
            <h2 className="text-lg font-semibold text-gray-900 mb-4">
              3. Captura de Selfie
            </h2>

            {/* Camera / Preview */}
            <div className="relative rounded-lg overflow-hidden bg-black mb-4" style={{ maxWidth: 480, margin: '0 auto' }}>
              {!photoPreview ? (
                <video
                  ref={videoRef}
                  autoPlay
                  playsInline
                  muted
                  aria-label="Câmera para selfie"
                  className="w-full"
                  style={{ transform: 'scaleX(-1)' }}
                />
              ) : (
                <img src={photoPreview} alt="Selfie" className="w-full" style={{ transform: 'scaleX(-1)' }} />
              )}
            </div>
            <canvas ref={canvasRef} className="hidden" />

            {!photoPreview ? (
              <div className="space-y-3">
                {!cameraFailed && (
                  <button
                    onClick={capturePhoto}
                    disabled={!cameraReady}
                    className="w-full bg-primary-600 text-white py-3 px-4 rounded-md font-medium hover:bg-primary-700 disabled:opacity-50"
                  >
                    {cameraReady ? 'Capturar Foto' : 'Iniciando câmera...'}
                  </button>
                )}

                {/* Fallback: file upload */}
                <input
                  ref={fileInputRef}
                  type="file"
                  accept="image/jpeg,image/png"
                  capture="user"
                  aria-label="Enviar foto para assinatura"
                  onChange={handleFileUpload}
                  className="hidden"
                />
                <button
                  onClick={() => fileInputRef.current?.click()}
                  className={`w-full py-3 px-4 rounded-md text-sm font-medium ${
                    cameraFailed
                      ? 'bg-primary-600 text-white hover:bg-primary-700'
                      : 'bg-gray-100 text-gray-700 hover:bg-gray-200 border border-gray-300'
                  }`}
                >
                  {cameraFailed ? 'Enviar Foto da Galeria' : 'Ou enviar foto da galeria'}
                </button>
              </div>
            ) : (
              <div className="space-y-4">
                <button
                  onClick={retakePhoto}
                  className="w-full bg-gray-200 text-gray-700 py-2 px-4 rounded-md text-sm font-medium hover:bg-gray-300"
                >
                  Tirar Outra Foto
                </button>

                {/* Consent */}
                <label className="flex items-start gap-3 p-4 bg-gray-50 rounded-lg cursor-pointer">
                  <input
                    type="checkbox"
                    checked={consent}
                    onChange={(e) => setConsent(e.target.checked)}
                    className="mt-1 h-4 w-4 text-primary-600 rounded border-gray-300 focus:ring-primary-500"
                  />
                  <span className="text-sm text-gray-700">
                    Declaro que li e concordo com o conteúdo do holerite apresentado.
                    Confirmo minha identidade por meio da selfie capturada neste momento.
                    Estou ciente de que meus dados pessoais (nome, CPF, foto e dados
                    técnicos) serão tratados conforme a{' '}
                    <a
                      href="/privacy"
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-primary-600 underline hover:text-primary-700"
                    >
                      Política de Privacidade
                    </a>{' '}
                    e a LGPD (Lei nº 13.709/2018).
                  </span>
                </label>

                <button
                  onClick={handleSign}
                  disabled={!consent || step === 'signing'}
                  className="w-full bg-green-600 text-white py-3 px-4 rounded-md font-semibold hover:bg-green-700 disabled:opacity-50"
                >
                  {step === 'signing' ? 'Assinando...' : 'Confirmar Assinatura'}
                </button>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
