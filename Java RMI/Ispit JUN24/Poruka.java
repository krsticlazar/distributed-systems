import java.io.Serializable;

public class Poruka implements Serializable {
    private static final long serialVersionUID = 1L;

    private final String naslov;
    private final String sadrzaj;

    public Poruka(String naslov, String sadrzaj) {
        this.naslov = naslov;
        this.sadrzaj = sadrzaj;
    }

    public String getNaslov() {
        return naslov;
    }

    public String getSadrzaj() {
        return sadrzaj;
    }

    public String toString() {
        return naslov + " - " + sadrzaj;
    }
}
