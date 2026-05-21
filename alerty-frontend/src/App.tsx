import type { Cliente } from "./types/Clientes";
import ClienteCard from "./components/ClienteCard";
import { useEffect, useState } from "react";
import { apiGet } from "./services/api";
const clientes: Cliente[] = [
  {
    id: 1,
    nome: "João Silva",
    telefone: "79999999999",
    status: "vencido",
    vencimento: "2026-05-10",
  },
  {
    id: 2,
    nome: "Maria Oliveira",
    telefone: "79988888888",
    status: "hoje",
    vencimento: "2026-05-13",
  },
  {
    id: 3,
    nome: "Carlos Souza",
    telefone: "79977777777",
    status: "em-dia",
    vencimento: "2026-05-20",
  },
];
function App() {
  const [busca, setBusca] = useState("");
  const [clientesApi, setClientesApi] = useState<Cliente[]>([]);
const [carregando, setCarregando] = useState(true);

useEffect(() => {
  async function carregarClientes() {
    try {
      const dados = await apiGet<Cliente[]>("/clientes");
      setClientesApi(dados);
    } catch (error) {
      console.error(error);
    } finally {
      setCarregando(false);
    }
  }

  carregarClientes();
}, []);
  const clientesFiltrados = clientesApi.filter((cliente) =>
  cliente.nome.toLowerCase().includes(busca.toLowerCase())
);
  return (
    <div className="app">
      <aside className="sidebar">
        <h1>Alerty</h1>

        <nav>
          <button>Meus clientes</button>
          <button>Relatório</button>
        </nav>
      </aside>

      <main className="content">
        <header className="topbar">
          <div>
            <h2>Meus clientes</h2>
            <p>Gerencie vencimentos, pagamentos e status dos alunos.</p>
          </div>

          <button className="primary-button">Cadastrar cliente</button>
        </header>

        <section className="cards">
          <div className="card vencido">
            <span>Vencidos</span>
            <strong>0</strong>
          </div>

          <div className="card hoje">
            <span>Vence hoje</span>
            <strong>0</strong>
          </div>

          <div className="card em-dia">
            <span>Em dia</span>
            <strong>0</strong>
          </div>
        </section>

        <section className="panel">
          <input
  placeholder="Buscar cliente..."
  value={busca}
  onChange={(e) => setBusca(e.target.value)}
/>

<div className="clientes-lista">
  {clientesFiltrados.map((cliente) => (
    <ClienteCard key={cliente.id} cliente={cliente} />
  ))}
</div>
        </section>
      </main>
    </div>
  );
}

export default App;