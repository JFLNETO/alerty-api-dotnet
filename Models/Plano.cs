public enum Plano
{
    Basico = 0,
    Medio = 1,
    Plus = 2
}

public static class PlanoLimites
{
    public static int MaxAlertas(Plano plano) => plano switch
    {
        Plano.Basico => 1,
        Plano.Medio => 3,
        Plano.Plus => 5,
        _ => 1
    };
}
