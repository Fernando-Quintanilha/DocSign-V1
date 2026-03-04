import { useQuery } from '@tanstack/react-query';
import { fetchPlans, fetchCurrentPlan } from '../../services/api';

interface Plan {
  id: string;
  name: string;
  displayName: string;
  maxDocuments: number;
  maxEmployees: number;
  priceMonthly: number;
}

export default function PlansPage() {
  const { data: plans, isLoading } = useQuery<Plan[]>({
    queryKey: ['plans'],
    queryFn: fetchPlans,
  });

  const { data: currentPlan } = useQuery<Plan>({
    queryKey: ['current-plan'],
    queryFn: fetchCurrentPlan,
  });

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <p className="text-gray-500">Carregando planos...</p>
      </div>
    );
  }

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Planos</h1>
        {currentPlan && (
          <p className="mt-1 text-sm text-gray-500">
            Seu plano atual: <span className="font-medium text-primary-600">{currentPlan.displayName}</span>
          </p>
        )}
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {plans?.map((plan) => {
          const isCurrent = currentPlan?.id === plan.id;
          return (
            <div
              key={plan.id}
              className={`bg-white rounded-lg shadow p-6 flex flex-col ${
                isCurrent ? 'ring-2 ring-primary-500' : ''
              }`}
            >
              {isCurrent && (
                <span className="text-xs font-semibold text-primary-600 bg-primary-50 px-2 py-1 rounded-full self-start mb-3">
                  Plano Atual
                </span>
              )}
              <h2 className="text-xl font-bold text-gray-900">{plan.displayName}</h2>
              <div className="mt-4">
                <span className="text-3xl font-bold text-gray-900">
                  {plan.priceMonthly === 0 ? 'Grátis' : `R$ ${plan.priceMonthly.toFixed(2)}`}
                </span>
                {plan.priceMonthly > 0 && (
                  <span className="text-sm text-gray-500">/mês</span>
                )}
              </div>
              <ul className="mt-6 space-y-3 flex-1">
                <li className="flex items-center text-sm text-gray-600">
                  <span className="text-green-500 mr-2">✓</span>
                  {plan.maxEmployees === -1 ? 'Funcionários ilimitados' : `Até ${plan.maxEmployees} funcionários`}
                </li>
                <li className="flex items-center text-sm text-gray-600">
                  <span className="text-green-500 mr-2">✓</span>
                  {plan.maxDocuments === -1 ? 'Documentos ilimitados/mês' : `Até ${plan.maxDocuments} documentos/mês`}
                </li>
                <li className="flex items-center text-sm text-gray-600">
                  <span className="text-green-500 mr-2">✓</span>
                  Assinatura com selfie
                </li>
                <li className="flex items-center text-sm text-gray-600">
                  <span className="text-green-500 mr-2">✓</span>
                  PDF assinado com QR Code
                </li>
                {plan.name !== 'free' && (
                  <>
                    <li className="flex items-center text-sm text-gray-600">
                      <span className="text-green-500 mr-2">✓</span>
                      Backup e exportação
                    </li>
                    <li className="flex items-center text-sm text-gray-600">
                      <span className="text-green-500 mr-2">✓</span>
                      Suporte prioritário
                    </li>
                  </>
                )}
              </ul>
              <div className="mt-6">
                {isCurrent ? (
                  <button
                    disabled
                    className="w-full py-2 px-4 rounded-md text-sm font-medium bg-gray-100 text-gray-500 cursor-not-allowed"
                  >
                    Plano Atual
                  </button>
                ) : (
                  <button
                    onClick={() => {
                      // In production, redirect to payment gateway
                      alert(`Para fazer upgrade para ${plan.displayName}, entre em contato com holeritesign.system@gmail.com`);
                    }}
                    className="w-full py-2 px-4 rounded-md text-sm font-medium bg-primary-600 text-white hover:bg-primary-700"
                  >
                    {plan.priceMonthly === 0 ? 'Selecionar' : 'Fazer Upgrade'}
                  </button>
                )}
              </div>
            </div>
          );
        })}
      </div>

      <div className="bg-blue-50 rounded-lg p-6 text-center">
        <h3 className="text-lg font-semibold text-blue-900">Precisa de um plano personalizado?</h3>
        <p className="mt-2 text-sm text-blue-700">
          Entre em contato conosco para planos Enterprise com funcionalidades customizadas.
        </p>
        <a
          href="mailto:holeritesign.system@gmail.com"
          className="mt-3 inline-block text-sm font-medium text-primary-600 hover:text-primary-700"
        >
          holeritesign.system@gmail.com
        </a>
      </div>
    </div>
  );
}
