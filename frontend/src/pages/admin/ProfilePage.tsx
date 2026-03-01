import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { useAuthStore } from '../../stores/auth';
import { changePassword, updateProfile } from '../../services/api';
import api from '../../lib/api';

export default function ProfilePage() {
  const admin = useAuthStore((s) => s.admin);
  const setAuth = useAuthStore((s) => s.setAuth);
  const token = useAuthStore((s) => s.token);
  const refreshToken = useAuthStore((s) => s.refreshToken);

  // Profile form
  const [name, setName] = useState(admin?.name || '');
  const [companyName, setCompanyName] = useState(admin?.companyName || '');
  const [profileMsg, setProfileMsg] = useState('');
  const [profileError, setProfileError] = useState('');

  // Password form
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [pwMsg, setPwMsg] = useState('');
  const [pwError, setPwError] = useState('');

  // Email verification
  const [verifyMsg, setVerifyMsg] = useState('');
  const [verifySending, setVerifySending] = useState(false);

  const profileMutation = useMutation({
    mutationFn: () => updateProfile(name, companyName),
    onSuccess: (data) => {
      setProfileMsg('Perfil atualizado com sucesso!');
      setProfileError('');
      if (token && refreshToken && admin) {
        setAuth(token, refreshToken, { ...admin, name: data.name, companyName: data.companyName });
      }
    },
    onError: (err: any) => {
      setProfileError(err.response?.data?.message || 'Erro ao atualizar perfil.');
      setProfileMsg('');
    },
  });

  const passwordMutation = useMutation({
    mutationFn: () => changePassword(currentPassword, newPassword),
    onSuccess: () => {
      setPwMsg('Senha alterada com sucesso!');
      setPwError('');
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
    },
    onError: (err: any) => {
      setPwError(err.response?.data?.message || 'Erro ao alterar senha.');
      setPwMsg('');
    },
  });

  const handleProfileSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setProfileMsg('');
    setProfileError('');
    if (!name.trim() || !companyName.trim()) {
      setProfileError('Preencha todos os campos.');
      return;
    }
    profileMutation.mutate();
  };

  const handlePasswordSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setPwMsg('');
    setPwError('');
    if (newPassword.length < 8) {
      setPwError('Nova senha deve ter no mínimo 8 caracteres.');
      return;
    }
    if (newPassword !== confirmPassword) {
      setPwError('As senhas não conferem.');
      return;
    }
    passwordMutation.mutate();
  };

  return (
    <div className="space-y-8">
      <h1 className="text-2xl font-bold text-gray-900">Meu Perfil</h1>

      {/* Email Verification Banner */}
      {admin && !admin.emailVerified && (
        <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <svg className="h-5 w-5 text-yellow-600 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-2.5L13.732 4c-.77-.833-1.964-.833-2.732 0L3.268 16.5c-.77.833.192 2.5 1.732 2.5z" />
            </svg>
            <div>
              <p className="text-sm font-medium text-yellow-800">E-mail não verificado</p>
              <p className="text-xs text-yellow-700">Verifique seu e-mail para ter acesso completo à plataforma.</p>
            </div>
          </div>
          <div className="flex items-center gap-2">
            {verifyMsg && <span className="text-xs text-green-600">{verifyMsg}</span>}
            <button
              onClick={async () => {
                setVerifySending(true);
                setVerifyMsg('');
                try {
                  await api.post('/auth/send-verification');
                  setVerifyMsg('E-mail enviado!');
                } catch {
                  setVerifyMsg('Erro ao enviar');
                } finally {
                  setVerifySending(false);
                }
              }}
              disabled={verifySending}
              className="px-3 py-1.5 bg-yellow-600 text-white text-xs font-medium rounded-md hover:bg-yellow-700 disabled:opacity-50 whitespace-nowrap"
            >
              {verifySending ? 'Enviando...' : 'Reenviar verificação'}
            </button>
          </div>
        </div>
      )}

      {/* Profile Info */}
      <div className="bg-white rounded-lg shadow p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Dados da Conta</h2>
        <form onSubmit={handleProfileSubmit} className="space-y-4 max-w-md">
          <div>
            <label htmlFor="profile-email" className="block text-sm font-medium text-gray-700">E-mail</label>
            <input
              id="profile-email"
              type="email"
              value={admin?.email || ''}
              disabled
              className="mt-1 block w-full rounded-md border border-gray-200 bg-gray-50 px-3 py-2 text-sm text-gray-500"
            />
          </div>
          <div>
            <label htmlFor="profile-name" className="block text-sm font-medium text-gray-700">Nome</label>
            <input
              id="profile-name"
              type="text"
              value={name}
              onChange={(e) => setName(e.target.value)}
              className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
            />
          </div>
          <div>
            <label htmlFor="profile-company" className="block text-sm font-medium text-gray-700">Empresa</label>
            <input
              id="profile-company"
              type="text"
              value={companyName}
              onChange={(e) => setCompanyName(e.target.value)}
              className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700">Plano</label>
            <p className="mt-1 text-sm text-primary-600 font-medium">{admin?.planName}</p>
          </div>
          {profileError && <p className="text-sm text-red-600">{profileError}</p>}
          {profileMsg && <p className="text-sm text-green-600">{profileMsg}</p>}
          <button
            type="submit"
            disabled={profileMutation.isPending}
            className="bg-primary-600 text-white px-4 py-2 rounded-md text-sm font-medium hover:bg-primary-700 disabled:opacity-50"
          >
            {profileMutation.isPending ? 'Salvando...' : 'Salvar Alterações'}
          </button>
        </form>
      </div>

      {/* Change Password */}
      <div className="bg-white rounded-lg shadow p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Alterar Senha</h2>
        <form onSubmit={handlePasswordSubmit} className="space-y-4 max-w-md">
          <div>
            <label htmlFor="pw-current" className="block text-sm font-medium text-gray-700">Senha Atual</label>
            <input
              id="pw-current"
              type="password"
              value={currentPassword}
              onChange={(e) => setCurrentPassword(e.target.value)}
              required
              className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
            />
          </div>
          <div>
            <label htmlFor="pw-new" className="block text-sm font-medium text-gray-700">Nova Senha</label>
            <input
              id="pw-new"
              type="password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              required
              minLength={8}
              className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
            />
          </div>
          <div>
            <label htmlFor="pw-confirm" className="block text-sm font-medium text-gray-700">Confirmar Nova Senha</label>
            <input
              id="pw-confirm"
              type="password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              required
              minLength={8}
              className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
            />
          </div>
          {pwError && <p className="text-sm text-red-600">{pwError}</p>}
          {pwMsg && <p className="text-sm text-green-600">{pwMsg}</p>}
          <button
            type="submit"
            disabled={passwordMutation.isPending}
            className="bg-primary-600 text-white px-4 py-2 rounded-md text-sm font-medium hover:bg-primary-700 disabled:opacity-50"
          >
            {passwordMutation.isPending ? 'Alterando...' : 'Alterar Senha'}
          </button>
        </form>
      </div>
    </div>
  );
}
