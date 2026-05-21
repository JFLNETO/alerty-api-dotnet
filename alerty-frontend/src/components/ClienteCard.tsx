import type { Cliente } from "../types/Clientes";

interface Props {
  cliente: Cliente;
}

function ClienteCard({ cliente }: Props) {
  return (
    <div className="cliente-card">
      <div>
        <h3>{cliente.nome}</h3>

        <p>{cliente.telefone}</p>

        <span className={`status ${cliente.status}`}>
          {cliente.status}
        </span>
      </div>

      <div className="acoes">
        <button>Pagar</button>
        <button>Editar</button>
      </div>
    </div>
  );
}

export default ClienteCard;