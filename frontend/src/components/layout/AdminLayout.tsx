import { useState } from 'react';
import { Outlet, Link, useNavigate } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import { useAuthStore } from '../../stores/auth';
import { useThemeStore } from '../../stores/theme';

export default function AdminLayout() {
  const admin = useAuthStore((s) => s.admin);
  const logout = useAuthStore((s) => s.logout);
  const dark = useThemeStore((s) => s.dark);
  const toggleTheme = useThemeStore((s) => s.toggle);
  const navigate = useNavigate();
  const qc = useQueryClient();
  const [mobileOpen, setMobileOpen] = useState(false);

  const handleLogout = () => {
    qc.clear();
    logout();
    navigate('/login');
  };

  const isSuperAdmin = admin?.role === 'SuperAdmin';

  const navLinks = [
    { to: '/admin', label: 'Dashboard' },
    { to: '/admin/employees', label: 'Funcionários' },
    { to: '/admin/documents', label: 'Documentos' },
    { to: '/admin/audit', label: 'Auditoria' },
    { to: '/admin/backup', label: 'Backup' },
    { to: '/admin/plans', label: 'Planos' },
    { to: '/admin/settings', label: 'Configurações' },
    { to: '/admin/whatsapp', label: 'WhatsApp' },
    ...(isSuperAdmin ? [{ to: '/admin/super', label: 'Super Admin' }] : []),
  ];

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Top Navbar */}
      <nav className="bg-white border-b border-gray-200 px-4 py-3">
        <div className="max-w-7xl mx-auto flex items-center justify-between">
          <div className="flex items-center gap-8">
            <Link to="/admin" className="text-xl font-bold text-primary-600">
              HoleriteSign
            </Link>
            {/* Desktop nav */}
            <div className="hidden lg:flex items-center gap-1 xl:gap-4 flex-wrap">
              {navLinks.map((link) => (
                <Link
                  key={link.to}
                  to={link.to}
                  className="text-sm font-medium text-gray-600 hover:text-gray-900 px-2 xl:px-3 py-2 rounded-md hover:bg-gray-100 whitespace-nowrap"
                >
                  {link.label}
                </Link>
              ))}
            </div>
          </div>
          <div className="flex items-center gap-4">
            <span className="hidden sm:inline text-sm text-gray-500">
              {admin?.companyName}
            </span>
            <button
              onClick={toggleTheme}
              className="p-2 rounded-md text-gray-500 hover:text-gray-700 hover:bg-gray-100 transition-colors"
              aria-label={dark ? 'Modo claro' : 'Modo escuro'}
              title={dark ? 'Modo claro' : 'Modo escuro'}
            >
              {dark ? (
                <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z" />
                </svg>
              ) : (
                <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z" />
                </svg>
              )}
            </button>
            <Link
              to="/admin/profile"
              className="hidden lg:inline text-sm font-medium text-gray-600 hover:text-gray-900 px-3 py-2 rounded-md hover:bg-gray-100"
            >
              Perfil
            </Link>
            <button
              onClick={handleLogout}
              className="hidden lg:inline text-sm font-medium text-red-600 hover:text-red-800 px-3 py-2 rounded-md hover:bg-red-50"
            >
              Sair
            </button>
            {/* Mobile hamburger */}
            <button
              onClick={() => setMobileOpen(!mobileOpen)}
              className="lg:hidden p-2 rounded-md text-gray-600 hover:bg-gray-100"
              aria-label="Menu"
            >
              {mobileOpen ? (
                <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              ) : (
                <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
                </svg>
              )}
            </button>
          </div>
        </div>

        {/* Mobile menu dropdown */}
        {mobileOpen && (
          <div className="lg:hidden mt-3 pt-3 border-t border-gray-200 space-y-1">
            {navLinks.map((link) => (
              <Link
                key={link.to}
                to={link.to}
                onClick={() => setMobileOpen(false)}
                className="block text-sm font-medium text-gray-600 hover:text-gray-900 px-3 py-2 rounded-md hover:bg-gray-100"
              >
                {link.label}
              </Link>
            ))}
            <Link
              to="/admin/profile"
              onClick={() => setMobileOpen(false)}
              className="block text-sm font-medium text-gray-600 hover:text-gray-900 px-3 py-2 rounded-md hover:bg-gray-100"
            >
              Perfil
            </Link>
            <button
              onClick={handleLogout}
              className="w-full text-left text-sm font-medium text-red-600 hover:text-red-800 px-3 py-2 rounded-md hover:bg-red-50"
            >
              Sair
            </button>
          </div>
        )}
      </nav>

      {/* Page Content */}
      <main className="max-w-7xl mx-auto py-6 px-4 sm:px-6 min-w-0 overflow-x-hidden">
        <Outlet />
      </main>
    </div>
  );
}
