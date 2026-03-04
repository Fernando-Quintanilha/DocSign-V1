import { useState, useEffect, useRef } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { fetchEmployees, fetchEmployeeDetail, createEmployee, updateEmployee, deleteEmployee, importEmployeesCsv } from '../../services/api';
import type { Employee } from '../../types';

const emptyForm = { name: '', email: '', whatsApp: '', cpf: '', birthDate: '' };

export default function EmployeesPage() {
  const queryClient = useQueryClient();
  const [showForm, setShowForm] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [error, setError] = useState('');
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [importResult, setImportResult] = useState<{ created: number; skipped: number; errors: string[] } | null>(null);
  const csvInputRef = useRef<HTMLInputElement>(null);

  // Debounce search input
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(search), 300);
    return () => clearTimeout(timer);
  }, [search]);

  const { data: employees = [], isLoading } = useQuery({
    queryKey: ['employees', debouncedSearch],
    queryFn: () => fetchEmployees(debouncedSearch || undefined),
  });

  const createMutation = useMutation({
    mutationFn: () => createEmployee({
      name: form.name,
      email: form.email || undefined,
      whatsApp: form.whatsApp || undefined,
      cpf: form.cpf || undefined,
      birthDate: form.birthDate || undefined,
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['employees'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard-enhanced'] });
      closeForm();
    },
    onError: (err: any) => {
      setError(err.response?.data?.message || 'Erro ao criar funcionário');
    },
  });

  const updateMutation = useMutation({
    mutationFn: () => updateEmployee(editingId!, {
      name: form.name,
      email: form.email || undefined,
      whatsApp: form.whatsApp || undefined,
      cpf: form.cpf || undefined,
      birthDate: form.birthDate || undefined,
    }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['employees'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard-enhanced'] });
      closeForm();
    },
    onError: (err: any) => {
      setError(err.response?.data?.message || 'Erro ao atualizar funcionário');
    },
  });

  const deleteMutation = useMutation({
    mutationFn: deleteEmployee,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['employees'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard-enhanced'] });
    },
  });

  const importMutation = useMutation({
    mutationFn: (file: File) => importEmployeesCsv(file),
    onSuccess: (data: any) => {
      queryClient.invalidateQueries({ queryKey: ['employees'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard-enhanced'] });
      setImportResult(data);
    },
    onError: (err: any) => {
      setError(err.response?.data?.message || 'Erro ao importar CSV');
    },
  });

  const handleCsvImport = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      setImportResult(null);
      setError('');
      importMutation.mutate(file);
    }
    if (csvInputRef.current) csvInputRef.current.value = '';
  };

  const closeForm = () => {
    setShowForm(false);
    setEditingId(null);
    setForm(emptyForm);
    setError('');
  };

  const openCreate = () => {
    setEditingId(null);
    setForm(emptyForm);
    setError('');
    setShowForm(true);
  };

  const openEdit = async (emp: Employee) => {
    setEditingId(emp.id);
    setForm({
      name: emp.name,
      email: emp.email || '',
      whatsApp: emp.whatsApp || '',
      cpf: '',
      birthDate: '',
    });
    setError('');
    setShowForm(true);

    // Fetch decrypted PII for pre-populating the form
    try {
      const detail = await fetchEmployeeDetail(emp.id);
      setForm(prev => ({
        ...prev,
        cpf: detail.cpf || '',
        birthDate: detail.birthDate || '',
      }));
    } catch {
      // If fetch fails, user can still edit other fields
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    if (editingId) {
      updateMutation.mutate();
    } else {
      createMutation.mutate();
    }
  };

  const isPending = createMutation.isPending || updateMutation.isPending;

  return (
    <div>
      <div className="flex items-center justify-between flex-wrap gap-2">
        <h1 className="text-2xl font-bold text-gray-900">Funcionários</h1>
        <div className="flex gap-2">
          <input
            ref={csvInputRef}
            type="file"
            accept=".csv"
            className="hidden"
            aria-label="Selecionar arquivo CSV para importação"
            onChange={handleCsvImport}
          />
          <button
            onClick={() => csvInputRef.current?.click()}
            disabled={importMutation.isPending}
            className="bg-green-600 text-white px-4 py-2 rounded-md text-sm font-medium hover:bg-green-700 disabled:opacity-50"
          >
            {importMutation.isPending ? 'Importando...' : '📥 Importar CSV'}
          </button>
          <button
            onClick={() => showForm ? closeForm() : openCreate()}
            className="bg-primary-600 text-white px-4 py-2 rounded-md text-sm font-medium hover:bg-primary-700"
          >
            {showForm ? 'Cancelar' : '+ Novo Funcionário'}
          </button>
        </div>
      </div>

      {/* Import Result */}
      {importResult && (
        <div className="mt-4 bg-blue-50 border border-blue-200 rounded-lg p-4">
          <p className="text-sm font-medium text-blue-800">
            Importação concluída: {importResult.created} criados, {importResult.skipped} ignorados
          </p>
          {importResult.errors.length > 0 && (
            <ul className="mt-2 text-xs text-red-600 list-disc list-inside">
              {importResult.errors.slice(0, 5).map((err, i) => (
                <li key={i}>{err}</li>
              ))}
              {importResult.errors.length > 5 && (
                <li>...e mais {importResult.errors.length - 5} erros</li>
              )}
            </ul>
          )}
          <button onClick={() => setImportResult(null)} className="mt-2 text-xs text-blue-600 hover:underline">
            Fechar
          </button>
        </div>
      )}

      {/* Search */}
      <div className="mt-4">
        <input
          type="text"
          aria-label="Buscar funcionários"
          placeholder="Buscar por nome, e-mail ou CPF..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="w-full max-w-md rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
        />
      </div>

      {/* Create / Edit Form */}
      {showForm && (
        <form onSubmit={handleSubmit} className="mt-4 bg-white shadow rounded-lg p-6 space-y-4">
          <h2 className="text-lg font-semibold text-gray-900">
            {editingId ? 'Editar Funcionário' : 'Novo Funcionário'}
          </h2>
          {error && <div className="bg-red-50 text-red-700 p-3 rounded-md text-sm">{error}</div>}

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label htmlFor="emp-name" className="block text-sm font-medium text-gray-700">Nome *</label>
              <input
                id="emp-name"
                type="text" required value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
                className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
              />
            </div>
            <div>
              <label htmlFor="emp-email" className="block text-sm font-medium text-gray-700">E-mail</label>
              <input
                id="emp-email"
                type="email" value={form.email}
                onChange={(e) => setForm({ ...form, email: e.target.value })}
                className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
              />
            </div>
            <div>
              <label htmlFor="emp-whatsapp" className="block text-sm font-medium text-gray-700">WhatsApp</label>
              <input
                id="emp-whatsapp"
                type="text" placeholder="+5511999999999" value={form.whatsApp}
                onChange={(e) => setForm({ ...form, whatsApp: e.target.value })}
                className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
              />
            </div>
            <div>
              <label htmlFor="emp-cpf" className="block text-sm font-medium text-gray-700">
                CPF
              </label>
              <input
                id="emp-cpf"
                type="text" placeholder="000.000.000-00" value={form.cpf}
                onChange={(e) => setForm({ ...form, cpf: e.target.value })}
                className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
              />
            </div>
            <div>
              <label htmlFor="emp-birthdate" className="block text-sm font-medium text-gray-700">
                Data de Nascimento
              </label>
              <input
                id="emp-birthdate"
                type="date" value={form.birthDate}
                onChange={(e) => setForm({ ...form, birthDate: e.target.value })}
                className="mt-1 block w-full rounded-md border border-gray-300 px-3 py-2 shadow-sm focus:border-primary-500 focus:outline-none focus:ring-1 focus:ring-primary-500"
              />
            </div>
          </div>

          <div className="flex gap-3">
            <button
              type="submit" disabled={isPending}
              className="bg-primary-600 text-white px-6 py-2 rounded-md text-sm font-medium hover:bg-primary-700 disabled:opacity-50"
            >
              {isPending ? 'Salvando...' : editingId ? 'Atualizar' : 'Salvar'}
            </button>
            <button
              type="button" onClick={closeForm}
              className="bg-gray-200 text-gray-800 px-6 py-2 rounded-md text-sm font-medium hover:bg-gray-300"
            >
              Cancelar
            </button>
          </div>
        </form>
      )}

      {/* Table */}
      <div className="mt-6 bg-white shadow rounded-lg overflow-x-auto">
        <table className="min-w-[700px] w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Nome</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">CPF (últimos 4)</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Contato</th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Status</th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Ações</th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {isLoading ? (
              <tr><td colSpan={5} className="px-6 py-12 text-center text-gray-500">Carregando...</td></tr>
            ) : employees.length === 0 ? (
              <tr><td colSpan={5} className="px-6 py-12 text-center text-gray-500">
                {search ? 'Nenhum funcionário encontrado.' : 'Nenhum funcionário cadastrado.'}
              </td></tr>
            ) : (
              employees.map((emp: Employee) => (
                <tr key={emp.id} className="hover:bg-gray-50">
                  <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                    <Link to={`/admin/employees/${emp.id}`} className="text-primary-600 hover:text-primary-800 hover:underline">
                      {emp.name}
                    </Link>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    {emp.cpfLast4 ? `***${emp.cpfLast4}` : '—'}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                    {emp.email || emp.whatsApp || '—'}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap">
                    <span className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${emp.isActive ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}`}>
                      {emp.isActive ? 'Ativo' : 'Inativo'}
                    </span>
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-right text-sm space-x-3">
                    <button
                      onClick={() => openEdit(emp)}
                      className="text-blue-600 hover:text-blue-800 font-medium"
                    >
                      Editar
                    </button>
                    <button
                      onClick={() => { if (confirm('Excluir funcionário?')) deleteMutation.mutate(emp.id); }}
                      className="text-red-600 hover:text-red-800 font-medium"
                    >
                      Excluir
                    </button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
