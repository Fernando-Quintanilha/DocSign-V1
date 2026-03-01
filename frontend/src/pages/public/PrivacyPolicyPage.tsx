export default function PrivacyPolicyPage() {
  return (
    <div className="min-h-screen bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-3xl mx-auto bg-white shadow rounded-lg p-8">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">
          Política de Privacidade
        </h1>
        <p className="text-sm text-gray-500 mb-8">
          Última atualização: {new Date().toLocaleDateString('pt-BR')}
        </p>

        <div className="space-y-6 text-gray-700 text-sm leading-relaxed">
          {/* 1. Introdução */}
          <section>
            <h2 className="text-lg font-semibold text-gray-900 mb-2">
              1. Introdução
            </h2>
            <p>
              Esta Política de Privacidade descreve como o sistema{' '}
              <strong>HoleriteSign</strong> coleta, utiliza, armazena e protege os
              dados pessoais dos usuários, em conformidade com a{' '}
              <strong>
                Lei Geral de Proteção de Dados Pessoais (Lei nº 13.709/2018 — LGPD)
              </strong>
              .
            </p>
          </section>

          {/* 2. Dados coletados */}
          <section>
            <h2 className="text-lg font-semibold text-gray-900 mb-2">
              2. Dados Pessoais Coletados
            </h2>
            <p>O sistema pode coletar os seguintes dados pessoais:</p>
            <ul className="list-disc pl-6 mt-2 space-y-1">
              <li>
                <strong>Dados de identificação:</strong> nome completo, CPF
                (últimos 4 dígitos para verificação), data de nascimento.
              </li>
              <li>
                <strong>Dados de contato:</strong> endereço de e-mail, número de
                WhatsApp.
              </li>
              <li>
                <strong>Dados biométricos (imagem):</strong> fotografia (selfie)
                capturada no momento da assinatura do holerite, utilizada como
                prova de identidade.
              </li>
              <li>
                <strong>Dados técnicos:</strong> endereço IP, user-agent do
                navegador, geolocalização (se autorizada), data e hora da
                assinatura.
              </li>
              <li>
                <strong>Dados financeiros:</strong> informações contidas nos
                holerites (demonstrativos de pagamento) enviados pelo
                empregador.
              </li>
            </ul>
          </section>

          {/* 3. Finalidade */}
          <section>
            <h2 className="text-lg font-semibold text-gray-900 mb-2">
              3. Finalidade do Tratamento
            </h2>
            <p>Os dados pessoais são tratados para as seguintes finalidades:</p>
            <ul className="list-disc pl-6 mt-2 space-y-1">
              <li>
                Possibilitar a assinatura digital de holerites pelo colaborador.
              </li>
              <li>
                Verificar a identidade do colaborador antes da assinatura.
              </li>
              <li>
                Gerar prova de assinatura com validade jurídica (trilha de
                auditoria).
              </li>
              <li>
                Permitir que o empregador acompanhe o status das assinaturas.
              </li>
              <li>
                Enviar notificações sobre holerites pendentes de assinatura.
              </li>
            </ul>
          </section>

          {/* 4. Base legal */}
          <section>
            <h2 className="text-lg font-semibold text-gray-900 mb-2">
              4. Base Legal para o Tratamento (Art. 7º, LGPD)
            </h2>
            <ul className="list-disc pl-6 mt-2 space-y-1">
              <li>
                <strong>Consentimento (Art. 7º, I):</strong> O colaborador
                manifesta livremente seu consentimento antes de assinar o
                holerite, incluindo a captura da selfie.
              </li>
              <li>
                <strong>
                  Cumprimento de obrigação legal ou regulatória (Art. 7º, II):
                </strong>{' '}
                Manutenção de registros trabalhistas conforme exigido pela
                legislação brasileira (CLT).
              </li>
              <li>
                <strong>
                  Exercício regular de direitos (Art. 7º, VI):
                </strong>{' '}
                Geração de provas de ciência do holerite pelo empregado, para
                eventuais processos judiciais ou administrativos.
              </li>
            </ul>
          </section>

          {/* 5. Compartilhamento */}
          <section>
            <h2 className="text-lg font-semibold text-gray-900 mb-2">
              5. Compartilhamento de Dados
            </h2>
            <p>
              Os dados pessoais <strong>não são compartilhados</strong> com
              terceiros, exceto:
            </p>
            <ul className="list-disc pl-6 mt-2 space-y-1">
              <li>
                Com o empregador que cadastrou o colaborador na plataforma, para
                fins de gestão dos holerites.
              </li>
              <li>
                Para cumprimento de obrigação legal, decisão judicial ou
                requisição de autoridade competente.
              </li>
            </ul>
          </section>

          {/* 6. Armazenamento e segurança */}
          <section>
            <h2 className="text-lg font-semibold text-gray-900 mb-2">
              6. Armazenamento e Segurança
            </h2>
            <ul className="list-disc pl-6 mt-2 space-y-1">
              <li>
                Os dados são armazenados em servidores protegidos com
                criptografia e controle de acesso.
              </li>
              <li>
                Cada empresa (tenant) possui isolamento lógico completo dos
                dados — nenhuma empresa tem acesso aos dados de outra.
              </li>
              <li>
                Todas as ações relevantes são registradas em trilha de auditoria
                imutável com hash encadeado (blockchain-like).
              </li>
              <li>
                Os documentos assinados são armazenados com integridade
                verificável (hash SHA-256).
              </li>
            </ul>
          </section>

          {/* 7. Retenção */}
          <section>
            <h2 className="text-lg font-semibold text-gray-900 mb-2">
              7. Retenção de Dados
            </h2>
            <p>
              Os dados pessoais e documentos assinados são retidos pelo prazo
              necessário ao cumprimento das obrigações legais trabalhistas
              (mínimo de <strong>5 anos</strong> conforme CLT, Art. 11), ou até
              que o titular solicite a eliminação, quando aplicável.
            </p>
          </section>

          {/* 8. Direitos do titular */}
          <section>
            <h2 className="text-lg font-semibold text-gray-900 mb-2">
              8. Direitos do Titular (Art. 18, LGPD)
            </h2>
            <p>O titular dos dados pessoais tem o direito de:</p>
            <ul className="list-disc pl-6 mt-2 space-y-1">
              <li>Confirmar a existência de tratamento de dados.</li>
              <li>Acessar seus dados.</li>
              <li>Corrigir dados incompletos, inexatos ou desatualizados.</li>
              <li>
                Solicitar a anonimização, bloqueio ou eliminação de dados
                desnecessários.
              </li>
              <li>
                Solicitar a portabilidade dos dados a outro fornecedor de
                serviço.
              </li>
              <li>
                Revogar o consentimento a qualquer momento (sem afetar a
                legalidade do tratamento anterior).
              </li>
            </ul>
            <p className="mt-2">
              Para exercer esses direitos, entre em contato com o seu empregador
              ou com o responsável pela plataforma.
            </p>
          </section>

          {/* 9. Cookies */}
          <section>
            <h2 className="text-lg font-semibold text-gray-900 mb-2">
              9. Cookies e Tecnologias Similares
            </h2>
            <p>
              O sistema utiliza apenas cookies essenciais para autenticação
              (token JWT). Não utilizamos cookies de rastreamento, analytics
              ou publicidade.
            </p>
          </section>

          {/* 10. Contato */}
          <section>
            <h2 className="text-lg font-semibold text-gray-900 mb-2">
              10. Contato e Encarregado (DPO)
            </h2>
            <p>
              Para dúvidas sobre esta Política de Privacidade, tratamento de
              dados pessoais ou exercício de direitos, entre em contato com o
              seu empregador ou com o administrador do sistema.
            </p>
          </section>
        </div>

        <div className="mt-8 pt-6 border-t border-gray-200">
          <a
            href="/"
            className="text-primary-600 hover:text-primary-700 text-sm font-medium"
          >
            ← Voltar
          </a>
        </div>
      </div>
    </div>
  );
}
