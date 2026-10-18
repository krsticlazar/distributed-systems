import java.io.Serializable;

public class Stock implements Serializable {
    private static final long serialVersionUID = 1L;

    private final int id;
    private final String companyName;
    private double price;

    public Stock(int id, String companyName, double price) {
        this.id = id;
        this.companyName = companyName;
        this.price = price;
    }

    public int getId() {
        return id;
    }

    public String getCompanyName() {
        return companyName;
    }

    public double getPrice() {
        return price;
    }

    public void setPrice(double price) {
        this.price = price;
    }

    public String getInfo() {
        return id + ": " + companyName + " - " + price;
    }

    public String toString() {
        return getInfo();
    }
}
