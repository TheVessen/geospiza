# Geospiza Client Site

A SvelteKit-based interface for evolutionary solvers, featuring real-time 3D visualization and data monitoring.

## Requirements

- Node.js 16+
- Grasshopper with Geospiza Plugin
- Web browser with WebGL support

## Key Features

- **WebSocket Communication:** Real-time bidirectional communication for solver control and status updates
- **3D Visualization:** Three.js integration for geometry previews
- **Data Charts:** Real-time data visualization using D3.js

## Architecture

### Client-Server Communication

- WebSocket protocol for bidirectional data flow
- Type-safe message passing using [SolverResponse](./src/lib/types.ts)
- Synchronized with [WebsocketComponent](../GeospizaPlugin/Src/AsyncComponent/WebsocketComponent.cs)

### Data Flow

```
GH_WebSolver -> WebSocket Server -> Client -> Three.js/D3.js Visualization
```

## Setup Instructions

1. Install dependencies:

```bash
npm install
```

2. Ensure prerequisites are running:

   - Grasshopper is active
   - [GH_WebSolver](../GeospizaPlugin/Components/Solvers/GH_WebSolver.cs) component is running
   - At least one [GH_WebIndividual](../GeospizaPlugin/Components/Webcomponents/GH_WebIndividual.cs) is connected (required for previews)

3. Start development server:

```bash
npm run dev
```

4. Verify connection:
   - A green outline indicates successful setup
   - Check WebSocket connection status in the UI

## Development

### Type Definitions

The [SolverResponse](./src/lib/types.ts) type mirrors the JSON structure from [\_collectClientData](../GeospizaPlugin/Src/AsyncComponent/WebsocketComponent.cs). Ensure both stay synchronized when making changes.

### Common Issues

1. **WebSocket Connection Fails**

   - Verify Grasshopper is running
   - Check if port is already in use
   - Ensure firewall isn't blocking connection

2. **Visualization Not Showing**

   - Verify WebGL is enabled
   - Check browser console for errors
   - Confirm GH_WebIndividual data format

3. **Zombie Process**

   ```bash
   # Windows
   netstat -ano | findstr :<port>
   taskkill /PID <process_id> /F

   # Mac/Linux
   lsof -i :<port>
   kill -9 <process_id>
   ```

## Related Components

- [WebSocketService](src/lib/websocket.ts) - Core communication handler
- [GH_WebSolver](../GeospizaPlugin/Components/Solvers/GH_WebSolver.cs) - Grasshopper solver component
- [GH_WebIndividual](../GeospizaPlugin/Components/Webcomponents/GH_WebIndividual.cs) - Individual preview component
