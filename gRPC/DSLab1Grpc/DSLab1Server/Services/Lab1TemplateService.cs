using DSLab1Server;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Diagnostics;
using System.Net;
using System.Xml.Linq;

namespace DSLab1Server.Services;

public class Lab1TemplateService : Lab1Service.Lab1ServiceBase
{
    private readonly AccState _accState;

    public Lab1TemplateService(AccState accState)
    {
        _accState = accState;
    }

    //Unary metoda - prima n, proverava da li je validan, postavlja na acc i vraca empty.
    //Unari metoda se pise Task<Empty> jer nema povratnu vrednost, samo prima FibRequest sto je broj tipa int 
    public override Task<Empty> PostaviAcc(FibRequest request, ServerCallContext context)
    {
        if (request.N < 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "n mora biti >= 0."));
        }

        int fibVrednost = IzracunajFibonacci(request.N);
        _accState.SetAcc(fibVrednost);

        return Task.FromResult(new Empty());
    }

    //Bidirekciona metoda - 1. uzima trenutno `acc`; 2. cita ulazni tok element po element; 3.broji redni broj svake pristigle vrednosti; 4. ako je element treci, sesti, deveti i tako dalje, mnozi ga sa `acc`; 5. inace od njega oduzima `acc`;6. svaku izracunatu vrednost odmah vraca klijentu
    //Bidirekciona metoda se pise Task jer je async, a ne vraca vrednost, nego samo koristi responseStream da vrati podatke klijentu
    public override async Task ObradiTok( IAsyncStreamReader<NumberData> requestStream, IServerStreamWriter<NumberData> responseStream, ServerCallContext context)
    {
        int acc = _accState.GetAcc();
        int redniBroj = 0;

        await foreach (var request in requestStream.ReadAllAsync())
        {
            redniBroj++;

            int rezultat;
            if (redniBroj % 3 == 0)
            {
                rezultat = request.Value * acc;
            }
            else
            {
                rezultat = request.Value - acc;
            }

            await responseStream.WriteAsync(new NumberData
            {
                Value = rezultat
            });
        }
    }

    private static int IzracunajFibonacci(int n)
    {
        if (n == 0)
        {
            return 0;
        }

        if (n == 1)
        {
            return 1;
        }

        int a = 0;
        int b = 1;

        for (int i = 2; i <= n; i++)
        {
            int c = a + b;
            a = b;
            b = c;
        }

        return b;
    }
}
