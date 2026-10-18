import java.io.Serializable;

public class Ticket implements Serializable {
    private static final long serialVersionUID = 1L;

    private final int number;
    private final String clientName;

    public Ticket(int number, String clientName) {
        this.number = number;
        this.clientName = clientName;
    }

    public int getNumber() {
        return number;
    }

    public String getClientName() {
        return clientName;
    }

    public String toString() {
        return number + " - " + clientName;
    }
}
