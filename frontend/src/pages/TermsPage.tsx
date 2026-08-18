export default function TermsPage() {
  return (
    <div className="lb-main" style={{ maxWidth: 760, margin: '0 auto' }}>
      <h1 style={{ fontSize: 24, marginBottom: 8 }}>LivingBank — Termos de Utilização</h1>
      <p className="lb-muted" style={{ marginBottom: 24 }}>Última atualização: 2026</p>

      <div style={{ lineHeight: 1.6 }}>
        <p>
          A LivingBank é uma ferramenta interna da Livingtours, disponibilizada apenas a
          utilizadores autorizados pela empresa, para consulta agregada de saldos e movimentos
          bancários através do Enable Banking.
        </p>

        <h2 style={{ fontSize: 18, marginTop: 20 }}>Acesso</h2>
        <p>
          O acesso à plataforma é restrito a colaboradores com conta criada por um administrador,
          sujeito a permissões (Admin, Manager, Viewer) que determinam as ações disponíveis.
        </p>

        <h2 style={{ fontSize: 18, marginTop: 20 }}>Ligação a contas bancárias</h2>
        <p>
          A ligação de uma conta bancária exige consentimento explícito do titular da conta,
          concedido diretamente no site/app do banco durante o fluxo de autorização do Enable
          Banking. A LivingBank nunca solicita nem processa credenciais bancárias diretamente.
        </p>

        <h2 style={{ fontSize: 18, marginTop: 20 }}>Limitações</h2>
        <p>
          As leituras automáticas de saldos/movimentos estão limitadas a um número máximo diário
          por conta, definido nas definições da plataforma, em conformidade com os limites
          impostos pelos bancos e pela regulação PSD2.
        </p>

        <h2 style={{ fontSize: 18, marginTop: 20 }}>Responsabilidade</h2>
        <p>
          A informação apresentada tem caráter informativo e reflete os dados devolvidos pelo
          Enable Banking; a Livingtours não se responsabiliza por indisponibilidades ou atrasos
          causados por terceiros (bancos ou Enable Banking).
        </p>

        <h2 style={{ fontSize: 18, marginTop: 20 }}>Contacto</h2>
        <p>Para questões sobre estes termos, contacta rui.neves@livingtours.com.</p>
      </div>
    </div>
  );
}
