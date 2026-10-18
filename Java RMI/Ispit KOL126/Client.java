import java.rmi.Naming;
import java.rmi.RemoteException;
import java.rmi.server.UnicastRemoteObject;

public class Client {
    public static void main(String[] args) {
        CallbackImpl callback = null;

        try {
            String host = args.length > 0 ? args[0] : "localhost";
            StockService service = (StockService) Naming.lookup(
                    "rmi://" + host + ":5099/StockService");

            System.out.println("Sve akcije:");
            for (Stock stock : service.getAllStocks()) {
                System.out.println(stock.getInfo());
            }

            callback = new CallbackImpl();
            service.subscribe(callback);
            System.out.println("Klijent prati promene cena. Pritisni Enter za kraj.");

            if (args.length > 1) {
                Thread.sleep(Long.parseLong(args[1]));
            } else {
                System.in.read();
            }
        } catch (Exception e) {
            e.printStackTrace();
        } finally {
            if (callback != null) {
                try {
                    UnicastRemoteObject.unexportObject(callback, true);
                } catch (Exception e) {
                }
            }
        }
    }

    private static class CallbackImpl extends UnicastRemoteObject implements Callback {
        public CallbackImpl() throws RemoteException {
            super();
        }

        public void priceChanged(int id, double newPrice) throws RemoteException {
            System.out.println("Akcija " + id + " ima novu cenu: " + newPrice);
        }
    }
}
