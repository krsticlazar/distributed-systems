import java.rmi.Naming;

public class AdminClient {
    public static void main(String[] args) {
        try {
            String host = args.length > 0 ? args[0] : "localhost";
            int id = args.length > 1 ? Integer.parseInt(args[1]) : 1;
            double newPrice = args.length > 2 ? Double.parseDouble(args[2]) : 125.5;

            StockService service = (StockService) Naming.lookup(
                    "rmi://" + host + ":5099/StockService");
            service.changePrice(id, newPrice);
            System.out.println("Promenjena je cena akcije " + id + ".");
        } catch (Exception e) {
            e.printStackTrace();
        }
    }
}
