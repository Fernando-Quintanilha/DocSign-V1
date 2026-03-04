import { useState } from 'react';
import { Outlet, Link, useNavigate } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import { useAuthStore } from '../../stores/auth';

export default function AdminLayout() {
  const admin = useAuthStore((s) => s.admin);
  const logout = useAuthStore((s) => s.logout);
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
