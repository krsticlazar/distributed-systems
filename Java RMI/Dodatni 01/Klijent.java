import java.rmi.Naming;

public class Klijent {
    public static void main(String[] args) {
        try {
            String host = args.length > 0 ? args[0] : "localhost";
            String biracId = args.length > 1 ? args[1] : "BIRAC-100";
            int kandidatId = args.length > 2 ? Integer.parseInt(args[2]) : 2;

            Glasanje glasanje = (Glasanje) Naming.lookup(
                    "rmi://" + host + ":1099/Glasanje");

            boolean uspeh = glasanje.glasaj(biracId, kandidatId);
            System.out.println("Glas evidentiran: " + uspeh);

            Rezultat rezultat = glasanje.vratiRezultat(kandidatId);
            System.out.println(rezultat);
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}
