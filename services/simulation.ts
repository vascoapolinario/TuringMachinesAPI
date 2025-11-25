
import { WorkshopItemDetail } from '../types';

export interface Tape {
  data: Record<number, string>;
  head: number;
}

export interface SimulationState {
  tape1: Tape;
  tape2: Tape;
  currentNodeId: number;
  steps: number;
  status: 'running' | 'accepted' | 'rejected' | 'stuck' | 'error';
  lastTransitionId: number | null; // index of connection in the array
  logs: string[];
}

export interface TestCaseResult {
  input: string;
  expected: string | boolean; // Output string for transform, boolean for accept
  actual: string | boolean;
  passed: boolean;
  logs: string[];
}

export class TuringSimulator {
  public nodes: any[];
  public connections: any[];
  private alphabet: string[];
  private maxSteps = 10000; 

  constructor(machine: WorkshopItemDetail) {
    this.nodes = JSON.parse(machine.nodesJson || '[]');
    this.connections = JSON.parse(machine.connectionsJson || '[]');
    this.alphabet = JSON.parse(machine.alphabetJson || '[]');
  }

  private createTape(input: string): Tape {
    const data: Record<number, string> = {};
    for (let i = 0; i < input.length; i++) {
      data[i] = input[i];
    }
    return { data, head: 0 };
  }

  public readTape(tape: Tape): string {
    return tape.data[tape.head] || '_';
  }

  private writeTape(tape: Tape, char: string | null) {
    if (char && char !== null) {
      tape.data[tape.head] = char;
    }
  }

  private moveHead(tape: Tape, direction: string) {
    if (direction === 'L') tape.head--;
    if (direction === 'R') tape.head++;
  }

  public getTapeContent(tape: Tape): string {
    const indices = Object.keys(tape.data).map(Number).sort((a, b) => a - b);
    if (indices.length === 0) return "";

    let min = indices[0];
    let max = indices[indices.length - 1];

    // Trim edges
    while (tape.data[min] === '_' && min <= max) min++;
    while (tape.data[max] === '_' && max >= min) max--;

    if (min > max) return "";

    let result = "";
    for (let i = min; i <= max; i++) {
      result += tape.data[i] || '_';
    }
    return result;
  }

  // --- Step-by-Step Logic ---

  public initSession(input: string, twoTapes: boolean): SimulationState {
    const startNode = this.nodes.find(n => n.is_start);
    if (!startNode) throw new Error("No start node found");

    return {
      tape1: this.createTape(input),
      tape2: this.createTape(""),
      currentNodeId: startNode.id,
      steps: 0,
      status: 'running',
      lastTransitionId: null,
      logs: [`Started at node ${startNode.id}`]
    };
  }

  public step(state: SimulationState, twoTapes: boolean): SimulationState {
    if (state.status !== 'running') return state;
    if (state.steps >= this.maxSteps) {
      return { ...state, status: 'error', logs: [...state.logs, 'Max steps reached'] };
    }

    const { currentNodeId, tape1, tape2 } = state;
    const read1 = this.readTape(tape1);
    const read2 = twoTapes ? this.readTape(tape2) : "_";

    // Find transition
    // We need the index to highlight it in visualizer
    const transitionIndex = this.connections.findIndex(c => {
      if (c.start !== currentNodeId) return false;
      
      const match1 = c.read && c.read.includes(read1);
      if (!match1) return false;

      if (twoTapes) {
          const match2 = c.read2 && c.read2.includes(read2);
          if (!match2) return false;
      }
      return true;
    });

    if (transitionIndex === -1) {
       // No transition possible. HALT.
       // Check if current node is end node
       const currentNode = this.nodes.find(n => n.id === currentNodeId);
       const isAccept = !!currentNode?.is_end;
       
       return {
         ...state,
         status: isAccept ? 'accepted' : 'rejected',
         logs: [...state.logs, `Halted at node ${currentNodeId}. Result: ${isAccept ? 'ACCEPT' : 'REJECT'}`]
       };
    }

    const transition = this.connections[transitionIndex];
    
    // Create copies for immutability
    const newTape1 = { ...tape1, data: { ...tape1.data } };
    const newTape2 = { ...tape2, data: { ...tape2.data } };

    // Execute
    this.writeTape(newTape1, transition.write);
    this.moveHead(newTape1, transition.move);

    if (twoTapes) {
      this.writeTape(newTape2, transition.write2);
      this.moveHead(newTape2, transition.move2);
    }

    return {
      tape1: newTape1,
      tape2: newTape2,
      currentNodeId: transition.end,
      steps: state.steps + 1,
      status: 'running',
      lastTransitionId: transitionIndex,
      logs: [...state.logs, `Transition: ${currentNodeId} -> ${transition.end}`] // Keep logs minimal or they explode
    };
  }

  // --- Full Run Wrapper ---

  public run(input: string, twoTapes: boolean): { finalState: string; output: string; isAccept: boolean; steps: number } {
    let state = this.initSession(input, twoTapes);
    while (state.status === 'running') {
      state = this.step(state, twoTapes);
    }

    const outputRaw = twoTapes ? this.getTapeContent(state.tape2) : this.getTapeContent(state.tape1);
    const isAccept = state.status === 'accepted';

    return { finalState: state.currentNodeId.toString(), output: outputRaw, isAccept, steps: state.steps };
  }

  public runLevelTests(level: WorkshopItemDetail): TestCaseResult[] {
    const results: TestCaseResult[] = [];
    const isTwoTapes = !!level.twoTapes;
    const mode = level.mode || 'transform'; // 'accept' or 'transform'

    let transformTests: any[] = [];
    let correctExamples: string[] = [];
    let wrongExamples: string[] = [];

    try {
      if (level.transformTestsJson) transformTests = JSON.parse(level.transformTestsJson);
      if (level.correctExamplesJson) correctExamples = JSON.parse(level.correctExamplesJson); 
      if (level.wrongExamplesJson) wrongExamples = JSON.parse(level.wrongExamplesJson);
    } catch (e) {
      console.error("Failed to parse level tests", e);
      return [];
    }

    if (mode === 'transform') {
      for (const test of transformTests) {
        const runRes = this.run(test.input, isTwoTapes);
        const passed = runRes.output === test.output;
        results.push({
          input: test.input,
          expected: test.output,
          actual: runRes.output,
          passed,
          logs: [`Steps: ${runRes.steps}`, `Final Node: ${runRes.finalState}`]
        });
      }
    } else {
      for (const input of correctExamples) {
        const runRes = this.run(input, isTwoTapes);
        results.push({
          input,
          expected: true,
          actual: runRes.isAccept,
          passed: runRes.isAccept === true,
          logs: [`Steps: ${runRes.steps}`, `Status: ${runRes.isAccept ? 'Accepted' : 'Rejected'}`]
        });
      }
      for (const input of wrongExamples) {
        const runRes = this.run(input, isTwoTapes);
        results.push({
          input,
          expected: false,
          actual: runRes.isAccept,
          passed: runRes.isAccept === false,
          logs: [`Steps: ${runRes.steps}`, `Status: ${runRes.isAccept ? 'Accepted' : 'Rejected'}`]
        });
      }
    }

    return results;
  }
}
