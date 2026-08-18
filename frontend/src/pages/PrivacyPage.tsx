export default function PrivacyPage() {
  return (
    <div className="lb-main" style={{ maxWidth: 760, margin: '0 auto' }}>
      <h1 style={{ fontSize: 24, marginBottom: 8 }}>LivingBank — Política de Privacidade</h1>
      <p className="lb-muted" style={{ marginBottom: 24 }}>Última atualização: 2026</p>

      <div style={{ lineHeight: 1.6 }}>
        <p>
          A LivingBank é uma aplicação interna da Livingtours para consulta agregada de saldos e
          movimentos de contas bancárias da empresa, através da API do Enable Banking (agregador
          licenciado ao abrigo da PSD2).
        </p>

        <h2 style={{ fontSize: 18, marginTop: 20 }}>Dados recolhidos</h2>
        <p>
          Saldos, movimentos e identificadores das contas bancárias autorizadas (IBAN, nome do
          banco), obtidos apenas após consentimento explícito dado diretamente ao banco através do
          Enable Banking. A LivingBank não recolhe nem armazena as credenciais de acesso ao
          homebanking — a autenticação é feita sempre diretamente no site/app do banco.
        </p>

        <h2 style={{ fontSize: 18, marginTop: 20 }}>Utilização dos dados</h2>
        <p>
          Os dados são usados exclusivamente para fins de consulta e controlo financeiro interno
          da empresa. Todas as operações realizadas na plataforma (acessos, leituras, alterações)
          ficam registadas em log de auditoria.
        </p>

        <h2 style={{ fontSize: 18, marginTop: 20 }}>Armazenamento</h2>
        <p>
          Os dados são guardados em base de dados PostgreSQL alojada na União Europeia, acessível
          apenas a utilizadores autenticados com permissões atribuídas por um administrador.
        </p>

        <h2 style={{ fontSize: 18, marginTop: 20 }}>Contacto</h2>
        <p>Para questões sobre esta política, contacta rui.neves@livingtours.com.</p>
      </div>
    </div>
  );
}
