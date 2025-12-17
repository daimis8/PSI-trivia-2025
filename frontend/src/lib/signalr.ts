import { HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { getApiUrl } from "@/lib/api";

export function createGameHub(): HubConnection {
  const connection = new HubConnectionBuilder()
    .withUrl(getApiUrl("/hubs/game"), {
      withCredentials: true,
    })
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Information)
    .build();
  return connection;
}
