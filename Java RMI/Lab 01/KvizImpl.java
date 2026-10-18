import java.rmi.RemoteException;
import java.rmi.server.UnicastRemoteObject;

public class KvizImpl extends UnicastRemoteObject implements Kviz {
    private final Pitanje[] pitanja;
    private final String[] tacniOdgovori;
    private int indeksTrenutnogPitanja;
    private int brojPoena;

    public KvizImpl() throws RemoteException {
        super();
        pitanja = new Pitanje[] {
            new PitanjeImpl("1+1= ?", "1", "2", "3"),
            new PitanjeImpl("2*3= ?", "6", "2", "1"),
            new PitanjeImpl("10/2= ?", "1", "2", "5")
        };
        tacniOdgovori = new String[] { "b", "a", "c" };
        indeksTrenutnogPitanja = 0;
        brojPoena = 0;
    }

    public synchronized void pocetak() throws RemoteException {
        brojPoena = 0;
        indeksTrenutnogPitanja = 0;
    }

    public synchronized Pitanje vratiPitanje() throws RemoteException {
        if (indeksTrenutnogPitanja >= pitanja.length) {
            return null;
        }
        return pitanja[indeksTrenutnogPitanja];
    }

    public synchronized void odgovori(String odg) throws RemoteException {
        if (indeksTrenutnogPitanja >= tacniOdgovori.length) {
            return;
        }

        String odgovor = odg == null ? "" : odg.trim();
        if (tacniOdgovori[indeksTrenutnogPitanja].equalsIgnoreCase(odgovor)) {
            brojPoena++;
        }
        indeksTrenutnogPitanja++;
    }

    public synchronized int vratiBrojPoena() throws RemoteException {
        return brojPoena;
    }
}
