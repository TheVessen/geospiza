<script>
  import { onMount } from "svelte";

  let messages = [];
  let socket;

  // This function runs once the component is mounted (client side)
  onMount(() => {
    // Create the WebSocket that points to our Grasshopper server
    socket = new WebSocket("ws://127.0.0.1:8181");

    // When the socket opens
    socket.onopen = () => {
      console.log("Connected to Grasshopper WebSocket");
      // Optionally, do an initial "status" request:
      // socket.send(JSON.stringify({ command: 'status' }));
    };

    // When we receive a message
    socket.onmessage = (event) => {
      // event.data is whatever Grasshopper sends back
      messages = [...messages, event.data];
    };

    // When an error occurs
    socket.onerror = (err) => {
      console.error("WebSocket error:", err);
      messages = [...messages, "WebSocket error: " + err.message];
    };

    // When the socket closes
    socket.onclose = () => {
      console.log("WebSocket closed");
      messages = [...messages, "WebSocket closed"];
    };
  });

  // Send a "run" command to Grasshopper
  function runSolver() {
    if (socket && socket.readyState === WebSocket.OPEN) {
      socket.send(JSON.stringify({ command: "run" }));
    } else {
      messages = [...messages, "WebSocket not connected"];
    }
  }

  // Send a "status" command to Grasshopper
  function getStatus() {
    if (socket && socket.readyState === WebSocket.OPEN) {
      socket.send(JSON.stringify({ command: "status" }));
    } else {
      messages = [...messages, "WebSocket not connected"];
    }
  }
</script>

<main>
  <h1>Remote Grasshopper Solver</h1>

  <button on:click={runSolver}>Run Solver</button>
  <button on:click={getStatus}>Get Status</button>

  <h3>Messages</h3>
  <ul>
    {#each messages as msg}
      <li>{msg}</li>
    {/each}
  </ul>
</main>
