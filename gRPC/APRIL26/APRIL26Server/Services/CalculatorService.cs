using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace APRIL26Server.Services;

public class CalculatorService : Calculator.CalculatorBase
{
    public override Task<RezultatResponse> Sabiranje(OperacijaRequest request, ServerCallContext context)
    {
        var message = new RezultatResponse
        {
            Operand1 = request.Operand1,
            Operand2 = request.Operand2,
            Operacija = "Sabiranje",
            Rezultat = request.Operand1 + request.Operand2,
        };
        return Task.FromResult(message);
    }
    public override Task<RezultatResponse> Oduzimanje(OperacijaRequest request, ServerCallContext context)
    {
        var message = new RezultatResponse
        {
            Operand1 = request.Operand1,
            Operand2 = request.Operand2,
            Operacija="Oduzimanje",
            Rezultat = request.Operand1 - request.Operand2,
        };
        return Task.FromResult(message);
    }
    public override Task<RezultatResponse> Mnozenje(OperacijaRequest request, ServerCallContext context)
    {
        var message = new RezultatResponse
        {
            Operand1 = request.Operand1,
            Operand2 = request.Operand2,
            Operacija = "Mnozenje",
            Rezultat = request.Operand1 * request.Operand2,
        };
        return Task.FromResult(message);
    }
    public override Task<RezultatResponse> Deljenje(OperacijaRequest request, ServerCallContext context)
    {
        var message = new RezultatResponse
        {
            Operand1 = request.Operand1,
            Operand2 = request.Operand2,
            Operacija = "Deljenje",
            Rezultat = request.Operand1 / request.Operand2,
        };
        return Task.FromResult(message);
    }
}
