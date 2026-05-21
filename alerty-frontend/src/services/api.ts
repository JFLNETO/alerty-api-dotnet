const API_BASE_URL = "http://localhost:5069";

export async function apiGet<T>(endpoint: string): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${endpoint}`);

  if (!response.ok) {
    throw new Error("Erro ao buscar dados da API");
  }

  return response.json();
}