import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { fetchProfile, updateProfile, changePassword } from '../../services/api';
import { useAuthStore } from '../../stores/auth';
import { useThemeStore } from '../../stores/theme';
import type { AdminProfile } from '../../types';

export default function SettingsPage() {
  const qc = useQueryClient();
  const setAuth = useAuthStore((s) => s.setAuth);
  const token = useAuthStore((s) => s.token);
  const refreshToken = useAuthStore((s) => s.refreshToken);
  const dark = useThemeStore((s) => s.dark);
  const toggleTheme = useThemeStore((s) => s.toggle);

  // Profile
  const [name, setName] = useState('');
  const [companyName, setCompanyName] = useState('');
  const [profileMsg, setProfileMsg] = useState('');

  // Password
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [passwordMsg, setPasswordMsg] = useState('');
  const [passwordError, setPasswordError] = useState('');

  const { data: profile, isLoading } = useQuery<AdminProfile>({
    queryKey: ['profile'],
    queryFn: fetchProfile,
  });

  // Initialize from profile
  if (profile && !name && !companyName) {
    setName(profile.name);
    setCompanyName(profile.companyName);
  }

  const profileMutation = useMutation({
    mutationFn: () => updateProfile(name, companyName),
    onSuccess: (data) => {
      setProfileMsg('Perfil atualizado com sucesso!');
      // Update auth store
      if (token && refreshToken) {
        setAuth(token, refreshToken, {
          id: data.id,
          name: data.name,
          email: data.email,
          companyName: data.companyName,
          role: data.role as 'Admin' | 'SuperAdmin',
          planName: data.planName,
          emailVerified: data.emailVerified,
        });
      }
      qc.invalidateQueries({ queryKey: ['profile'] });
      setTimeout(() => setProfileMsg(''), 3000);
    },
    onError: (err: unknown) => {
      setProfileMsg('');
      const message = (err as { response?: { data?: { message?: string } } })?.response?.data?.message || 'Erro ao atualizar perfil';
      alert(message);
    },
  });

  const passwordMutation = useMutation({
    mutationFn: () => changePassword(currentPassword, newPassword),
    onSuccess: () => {
      setPasswordMsg('Senha alterada com sucesso!');
      setPasswordError('');
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
      setTimeout(() => setPasswordMsg(''), 3000);
    },
    onError: (err: unknown) => {
      const message = (err as { response?: { data?: { message?: string } } })?.response?.data?.message || 'Erro ao alterar senha';
      setPasswordError(message);
      setPasswordMsg('');
    },
  });

  const handleProfileSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    profileMutation.mutate();
  };

  const handlePasswordSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setPasswordError('');
    if (newPassword.length < 8) {
      setPasswordError('A nova senha deve ter pelo menos 8 caracteres.');
      return;
    }
    if (newPassword !== confirmPassword) {
      setPasswordError('As senhas não coincidem.');
      return;
    }
    passwordMutation.mutate();
  };

  if (isLoading) {
    return <div className="py-12 text-center text-gray-500">Carregando...</div>;
  }

  return (
    <div className="space-y-8 max-w-2xl">
      <h1 className="text-2xl font-bold text-gray-900">Configurações</h1>

      {/* Appearance / Dark Mode Section */}
      <div className="bg-white rounded-lg shadow p-6">
        <h2 className="text-lg font-semibold text-gray-900 mb-4">Aparência</h2>
        <div className="flex items-center justify-between">
          <div>
            <p className="text-sm font-medium text-gray-700">Modo Escuro</p>
            <p className="text-xs text-gray-500 mt-1">Alterne entre o tema claro e escuro</p>
          </div>
          <button
            onClick={toggleTheme}
            className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 ${
              dark ? 'bg-primary-600' : 'bg-gray-300'
            }`}
            role="switch"
            aria-checked={dark}
            aria-label="Ativar modo escuro"
          >
            <span
              className={`inline-block h-4 w-4 transform rounded-full bg-white transition-transform ${
                dark ? 'translate-x-6' : 'translate-x-1'
              }`}
            />
          </button>
        </div>
      </div>

      {/* Profile Section */}
      <form onSubmit={handleProfileSubmit} className="bg-white rounded-lg shadow p-6 space-y-4">
        <h2 className="text-lg font-semibold text-gray-900">Dados da Empresa</h2>

        {profileMsg && (
          <div className="bg-green-50 text-green-700 p-3 rounded-md text-sm">{profileMsg}</div>
        )}

        <div>
          <label htmlFor="admin-name" className="block text-sm font-medium text-gray-700">Nome do Administrador</label>
          <input
            id="admin-name"
            type="text"
            required
            value={name}
            onChange={(e) => setName(e.target.value)}
            className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
          />
        </div>
        <div>
          <label htmlFor="company-name" className="block text-sm font-medium text-gray-700">Nome da Empresa</label>
          <input
            id="company-name"
            type="text"
            required
            value={companyName}
            onChange={(e) => setCompanyName(e.target.value)}
            className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
          />
        </div>
        <div>
          <label htmlFor="settings-email" className="block text-sm font-medium text-gray-700">E-mail</label>
          <input
            id="settings-email"
            type="email"
            disabled
            value={profile?.email || ''}
            className="mt-1 block w-full rounded-md border border-gray-200 bg-gray-50 px-3 py-2 text-gray-500"
          />
          <p className="mt-1 text-xs text-gray-400">O e-mail não pode ser alterado.</p>
        </div>

        <button
          type="submit"
          disabled={profileMutation.isPending}
          className="bg-primary-600 text-white px-6 py-2 rounded-md text-sm font-medium hover:bg-primary-700 disabled:opacity-50"
        >
          {profileMutation.isPending ? 'Salvando...' : 'Salvar Alterações'}
        </button>
      </form>

      {/* Password Section */}
      <form onSubmit={handlePasswordSubmit} className="bg-white rounded-lg shadow p-6 space-y-4">
        <h2 className="text-lg font-semibold text-gray-900">Alterar Senha</h2>

        {passwordMsg && (
          <div className="bg-green-50 text-green-700 p-3 rounded-md text-sm">{passwordMsg}</div>
        )}
        {passwordError && (
          <div className="bg-red-50 text-red-700 p-3 rounded-md text-sm">{passwordError}</div>
        )}

        <div>
          <label htmlFor="current-password" className="block text-sm font-medium text-gray-700">Senha Atual</label>
          <input
            id="current-password"
            type="password"
            required
            value={currentPassword}
            onChange={(e) => setCurrentPassword(e.target.value)}
            className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
          />
        </div>
        <div>
          <label htmlFor="new-password" className="block text-sm font-medium text-gray-700">Nova Senha</label>
          <input
            id="new-password"
            type="password"
            required
            minLength={8}
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
          />
        </div>
        <div>
          <label htmlFor="confirm-password" className="block text-sm font-medium text-gray-700">Confirmar Nova Senha</label>
          <input
            id="confirm-password"
            type="password"
            required
            minLength={8}
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
            className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
          />
        </div>

        <button
          type="submit"
          disabled={passwordMutation.isPending}
          className="bg-red-600 text-white px-6 py-2 rounded-md text-sm font-medium hover:bg-red-700 disabled:opacity-50"
        >
          {passwordMutation.isPending ? 'Alterando...' : 'Alterar Senha'}
        </button>
      </form>

      {/* Plan Info */}
      {profile && (
        <div className="bg-white rounded-lg shadow p-6">
          <h2 className="text-lg font-semibold text-gray-900 mb-3">Informações do Plano</h2>
          <div className="space-y-2 text-sm text-gray-600">
            <p><span className="font-medium text-gray-800">Plano:</span> {profile.planName}</p>
            <p><span className="font-medium text-gray-800">Role:</span> {profile.role}</p>
            <p>
              <span className="font-medium text-gray-800">E-mail verificado:</span>{' '}
              {profile.emailVerified ? (
                <span className="text-green-600">Sim</span>
              ) : (
                <span className="text-red-600">Não</span>
              )}
            </p>
          </div>
        </div>
      )}
    </div>
  );
}
