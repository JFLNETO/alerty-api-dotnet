export interface Cliente {
  id: number;
  nome: string;
  telefone: string;
  status: "vencido" | "hoje" | "em-dia";
  vencimento: string;
}