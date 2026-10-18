import java.util.*;

public class PrijavaImpl implements Prijava {
    private static final long serialVersionUID = 1L;
    private ArrayList<String> ispiti;

    public PrijavaImpl() {
        ispiti = new ArrayList<String>();
    }

    public void dodajIspit(String ispit) {
        ispiti.add(ispit);
    }

    public String vratiIspite() {
        if (ispiti.isEmpty()) {
            return "Nema prijavljenih ispita.";
        }

        String rezultat = "";
        for (int i = 0; i < ispiti.size(); i++) {
            rezultat += (i + 1) + ". " + ispiti.get(i) + "\n";
        }
        return rezultat;
    }
}
