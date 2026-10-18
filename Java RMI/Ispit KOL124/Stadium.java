import java.io.Serializable;

public class Stadium implements Serializable {
    private static final long serialVersionUID = 1L;

    private final String name;
    private final String city;

    public Stadium(String name, String city) {
        this.name = name;
        this.city = city;
    }

    public String getName() {
        return name;
    }

    public String getCity() {
        return city;
    }

    public String toString() {
        return name + ", " + city;
    }
}
