import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from '../../lib/api';

interface Account {
  id: string;
  name: string;
  email: string;
  companyName: string;
  role: string;
  planName: string;
  planId: string;
  emailVerified: boolean;
  isActive: boolean;
  createdAt: string;
  employeeCount: number;
  documentCount: number;
}

interface Plan {
  id: string;
  name: string;
  displayName: string;
  maxDocuments: number;
  maxEmployees: number;
  priceMonthly: number;
  isActive: boolean;
  adminCount: number;
}

interface Metrics {
  totalAccounts: number;
  activeAccounts: number;
  totalEmployees: number;
  totalDocuments: number;
  signedDocuments: number;
  pendingDocuments: number;
  docsThisMonth: number;
  planDistribution: { plan: string; count: number }[];
}

type Tab = 'metrics' | 'accounts' | 'plans';

export default function SuperAdminPage() {
  const [tab, setTab] = useState<Tab>('metrics');
  const [search, setSearch] = useState('');
  const [editingAccount, setEditingAccount] = useState<Account | null>(null);
  const [editPlanId, setEditPlanId] = useState('');
  const [editIsActive, setEditIsActive] = useState(true);
  const [editRole, setEditRole] = useState('Admin');
  const qc = useQueryClient();

  // ── Queries ────────────────────
  const metricsQuery = useQuery<Metrics>({
    queryKey: ['super-metrics'],
    queryFn: () => api.get('/super/metrics').then(r => r.data),
    enabled: tab === 'metrics',
  });

  const accountsQuery = useQuery<{ total: number; accounts: Account[] }>({
    queryKey: ['super-accounts', search],
    queryFn: () => api.get('/super/accounts', { params: { search, pageSize: 100 } }).then(r => r.data),
    enabled: tab === 'accounts',
  });

  const plansQuery = useQuery<Plan[]>({
    queryKey: ['super-plans'],
    queryFn: () => api.get('/super/plans').then(r => r.data),
    enabled: tab === 'accounts' || tab === 'plans',
  });

  const updateAccountMutation = useMutation({
    mutationFn: (data: { id: string; planId?: string; isActive?: boolean; role?: string }) =>
      api.put(`/super/accounts/${data.id}`, {
        planId: data.planId || undefined,
        isActive: data.isActive,
        role: data.role,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['super-accounts'] });
      qc.invalidateQueries({ queryKey: ['super-metrics'] });
      setEditingAccount(null);
    },
  });

  const tabs: { key: Tab; label: string }[] = [
    { key: 'metrics', label: 'Métricas' },
    { key: 'accounts', label: 'Contas' },
    { key: 'plans', label: 'Planos' },
  ];

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-gray-900">Super Admin</h1>

      {/* Tabs */}
      <div className="border-b border-gray-200">
        <nav className="flex gap-4">
          {tabs.map(t => (
            <button
              key={t.key}
              onClick={() => setTab(t.key)}
              className={`pb-3 px-1 text-sm font-medium border-b-2 ${
                tab === t.key
                  ? 'border-primary-500 text-primary-600'
                  : 'border-transparent text-gray-500 hover:text-gray-700'
              }`}
            >
              {t.label}
            </button>
          ))}
        </nav>
      </div>

      {/* ── Metrics Tab ── */}
      {tab === 'metrics' && metricsQuery.data && (
        <div className="space-y-6">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            {[
              { label: 'Total Contas', value: metricsQuery.data.totalAccounts },
              { label: 'Contas Ativas', value: metricsQuery.data.activeAccounts },
              { label: 'Total Funcionários', value: metricsQuery.data.totalEmployees },
              { label: 'Total Documentos', value: metricsQuery.data.totalDocuments },
              { label: 'Assinados', value: metricsQuery.data.signedDocuments },
              { label: 'Pendentes', value: metricsQuery.data.pendingDocuments },
              { label: 'Docs este mês', value: metricsQuery.data.docsThisMonth },
            ].map(s => (
              <div key={s.label} className="bg-white rounded-lg shadow p-4">
                <p className="text-xs text-gray-500 uppercase">{s.label}</p>
                <p className="text-2xl font-bold text-gray-900">{s.value}</p>
              </div>
            ))}
          </div>

          {metricsQuery.data.planDistribution.length > 0 && (
            <div className="bg-white rounded-lg shadow p-6">
              <h3 className="text-sm font-semibold text-gray-700 mb-3">Distribuição por Plano</h3>
              <div className="space-y-2">
                {metricsQuery.data.planDistribution.map(p => (
                  <div key={p.plan} className="flex items-center justify-between">
                    <span className="text-sm text-gray-600">{p.plan}</span>
                    <span className="text-sm font-medium text-gray-900">{p.count} contas</span>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      )}

      {/* ── Accounts Tab ── */}
      {tab === 'accounts' && (
        <div className="space-y-4">
          <input
            type="text"
            placeholder="Buscar por nome, email ou empresa..."
            value={search}
            onChange={e => setSearch(e.target.value)}
            className="w-full max-w-md rounded-md border border-gray-300 px-3 py-2 text-sm shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
          />

          <div className="bg-white rounded-lg shadow overflow-hidden">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Empresa</th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Admin</th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">Plano</th>
                  <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">Func.</th>
                  <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">Docs</th>
                  <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">Status</th>
                  <th className="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase">Ações</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-200">
                {accountsQuery.data?.accounts.map(a => (
                  <tr key={a.id}>
                    <td className="px-4 py-3 text-sm text-gray-900">{a.companyName}</td>
                    <td className="px-4 py-3">
                      <p className="text-sm text-gray-900">{a.name}</p>
                      <p className="text-xs text-gray-500">{a.email}</p>
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-600">{a.planName}</td>
                    <td className="px-4 py-3 text-sm text-center text-gray-600">{a.employeeCount}</td>
                    <td className="px-4 py-3 text-sm text-center text-gray-600">{a.documentCount}</td>
                    <td className="px-4 py-3 text-center">
                      <span className={`inline-flex px-2 py-0.5 rounded-full text-xs font-medium ${
                        a.isActive ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'
                      }`}>
                        {a.isActive ? 'Ativo' : 'Inativo'}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-center">
                      <button
                        onClick={() => {
                          setEditingAccount(a);
                          setEditPlanId(a.planId);
                          setEditIsActive(a.isActive);
                          setEditRole(a.role);
                        }}
                        className="text-xs text-primary-600 hover:text-primary-800 font-medium"
                      >
                        Editar
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* ── Plans Tab ── */}
      {tab === 'plans' && plansQuery.data && (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          {plansQuery.data.map(p => (
            <div key={p.id} className={`bg-white rounded-lg shadow p-5 border-2 ${p.isActive ? 'border-transparent' : 'border-red-200 opacity-60'}`}>
              <h3 className="font-semibold text-gray-900">{p.displayName}</h3>
              <p className="text-2xl font-bold text-primary-600 mt-1">
                {p.priceMonthly === 0 ? 'Grátis' : `R$ ${p.priceMonthly.toFixed(2)}/mês`}
              </p>
              <div className="mt-3 space-y-1 text-sm text-gray-600">
                <p>{p.maxEmployees === -1 ? 'Funcionários ilimitados' : `Até ${p.maxEmployees} funcionários`}</p>
                <p>{p.maxDocuments === -1 ? 'Documentos ilimitados' : `Até ${p.maxDocuments} docs/mês`}</p>
              </div>
              <p className="mt-3 text-xs text-gray-400">{p.adminCount} contas usando</p>
            </div>
          ))}
        </div>
      )}

      {/* ── Edit Account Modal ── */}
      {editingAccount && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50" onClick={() => setEditingAccount(null)}>
          <div className="bg-white rounded-lg shadow-xl p-6 w-full max-w-md" onClick={e => e.stopPropagation()}>
            <h2 className="text-lg font-semibold text-gray-900 mb-4">
              Editar: {editingAccount.companyName}
            </h2>
            <div className="space-y-4">
              <div>
                <label htmlFor="edit-plan" className="block text-sm font-medium text-gray-700">Plano</label>
                <select
                  id="edit-plan"
                  value={editPlanId}
                  onChange={e => setEditPlanId(e.target.value)}
                  className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
                >
                  {plansQuery.data?.map(p => (
                    <option key={p.id} value={p.id}>{p.displayName}</option>
                  ))}
                </select>
              </div>
              <div>
                <label htmlFor="edit-role" className="block text-sm font-medium text-gray-700">Role</label>
                <select
                  id="edit-role"
                  value={editRole}
                  onChange={e => setEditRole(e.target.value)}
                  className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 text-sm"
                >
                  <option value="Admin">Admin</option>
                  <option value="SuperAdmin">SuperAdmin</option>
                </select>
              </div>
              <div className="flex items-center gap-2">
                <input
                  id="active"
                  type="checkbox"
                  checked={editIsActive}
                  onChange={e => setEditIsActive(e.target.checked)}
                  className="rounded border-gray-300"
                />
                <label htmlFor="active" className="text-sm text-gray-700">Conta ativa</label>
              </div>
            </div>
            <div className="mt-6 flex justify-end gap-3">
              <button
                onClick={() => setEditingAccount(null)}
                className="px-4 py-2 text-sm text-gray-600 hover:text-gray-800"
              >
                Cancelar
              </button>
              <button
                onClick={() => {
                  updateAccountMutation.mutate({
                    id: editingAccount.id,
                    planId: editPlanId,
                    isActive: editIsActive,
                    role: editRole,
                  });
                }}
                disabled={updateAccountMutation.isPending}
                className="px-4 py-2 bg-primary-600 text-white text-sm font-medium rounded-md hover:bg-primary-700 disabled:opacity-50"
              >
                {updateAccountMutation.isPending ? 'Salvando...' : 'Salvar'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
