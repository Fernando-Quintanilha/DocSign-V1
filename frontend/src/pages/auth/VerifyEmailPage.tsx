import { useEffect, useState } from 'react';
import { useSearchParams, Link } from 'react-router-dom';
import api from '../../lib/api';

export default function VerifyEmailPage() {
  const [searchParams] = useSearchParams();
  const tokenFromUrl = searchParams.get('token') || '';

  const [status, setStatus] = useState<'loading' | 'success' | 'error'>('loading');
  const [message, setMessage] = useState('');

  useEffect(() => {
    if (!tokenFromUrl) {
      setStatus('error');
      setMessage('Token de verificação não encontrado no link.');
      return;
    }

    const verify = async () => {
      try {
        await api.post('/auth/verify-email', { token: tokenFromUrl });
        setStatus('success');
        setMessage('Seu e-mail foi verificado com sucesso!');
      } catch (err: any) {
        setStatus('error');
        setMessage(err.response?.data?.message || 'Token inválido ou expirado.');
      }
    };

    verify();
  }, [tokenFromUrl]);

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4">
      <div className="max-w-md w-full bg-white p-8 rounded-lg shadow text-center space-y-4">
        <div className="text-center mb-4">
          <h1 className="text-3xl font-bold text-primary-600">HoleriteSign</h1>
        </div>

        {status === 'loading' && (
          <>
            <div className="mx-auto flex items-center justify-center h-12 w-12">
              <svg className="animate-spin h-8 w-8 text-primary-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
              </svg>
            </div>
            <h2 className="text-lg font-medium text-gray-900">Verificando e-mail...</h2>
          </>
        )}

        {status === 'success' && (
          <>
            <div className="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-green-100">
              <svg className="h-6 w-6 text-green-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
              </svg>
            </div>
            <h2 className="text-lg font-medium text-gray-900">E-mail verificado!</h2>
            <p className="text-sm text-gray-600">{message}</p>
            <Link
              to="/admin"
              className="inline-block mt-4 px-4 py-2 bg-primary-600 text-white text-sm font-medium rounded-md hover:bg-primary-700"
            >
              Ir para o painel
            </Link>
          </>
        )}

        {status === 'error' && (
          <>
            <div className="mx-auto flex items-center justify-center h-12 w-12 rounded-full bg-red-100">
              <svg className="h-6 w-6 text-red-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </div>
            <h2 className="text-lg font-medium text-gray-900">Verificação falhou</h2>
            <p className="text-sm text-gray-600">{message}</p>
            <Link
              to="/login"
              className="inline-block mt-4 text-sm font-medium text-primary-600 hover:text-primary-500"
            >
              Voltar ao login
            </Link>
          </>
        )}
      </div>
    </div>
  );
}
