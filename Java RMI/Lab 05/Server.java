import java.rmi.Naming;
import java.rmi.registry.LocateRegistry;
import java.rmi.server.ExportException;

public class Server {
    public static void main(String[] args) {
        try {
            try {
                LocateRegistry.createRegistry(1099);                                                               // Pokretanje registry-ja ako vec nije podignut.
            } catch (ExportException e) {                                                                               // Ako vec postoji, nastavljamo bez prekida.
            }

            EAukcija aukcija = new EAukcijaImpl();                                                                      // Glavni objekat sistema aukcije.
            Naming.rebind("rmi://localhost/EAukcija", aukcija);                                                   // Registracija pod imenom koje klijent koristi u lookup-u.

            System.out.println("Server elektronske aukcije je pokrenut.");
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}
