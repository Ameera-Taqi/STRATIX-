import { TaskCard } from '../../core/models/task.model';

export type TaskStatus = TaskCard['status'];

const ALLOWED: Record<TaskStatus, readonly TaskStatus[]> = {
  TODO: ['IN_PROGRESS', 'BLOCKED'],
  IN_PROGRESS: ['REVIEW', 'BLOCKED', 'TODO', 'DONE'],
  REVIEW: ['DONE', 'IN_PROGRESS', 'BLOCKED'],
  DONE: ['IN_PROGRESS', 'TODO'],
  BLOCKED: ['TODO', 'IN_PROGRESS'],
};

export type TransitionRequirement = 'blockedReason' | 'reopenReason' | 'requestChanges';

export interface TransitionAnalysis {
  allowed: boolean;
  /** Present when a reason must be collected before calling the API. */
  requirement?: TransitionRequirement;
  /** i18n key explaining why the drop/move is not allowed. */
  messageKey?: string;
  /** Optional one-click recovery status (e.g. Move to In Progress). */
  suggestedStatus?: TaskStatus;
  suggestedActionKey?: string;
}

/** Mirrors backend TaskTransitionRules — keep in sync. */
export function canTransition(from: TaskStatus, to: TaskStatus): boolean {
  return from === to || ALLOWED[from].includes(to);
}

export function analyzeTaskTransition(from: TaskStatus, to: TaskStatus): TransitionAnalysis {
  if (from === to) return { allowed: true };

  if (!canTransition(from, to)) {
    return illegalHint(from, to);
  }

  if (to === 'BLOCKED') {
    return { allowed: true, requirement: 'blockedReason' };
  }

  if (from === 'DONE') {
    return { allowed: true, requirement: 'reopenReason' };
  }

  // Request Changes: REVIEW → IN_PROGRESS requires reviewer feedback.
  if (from === 'REVIEW' && to === 'IN_PROGRESS') {
    return { allowed: true, requirement: 'requestChanges' };
  }

  return { allowed: true };
}

function illegalHint(from: TaskStatus, to: TaskStatus): TransitionAnalysis {
  if (from === 'TODO' && to === 'DONE') {
    return {
      allowed: false,
      messageKey: 'tasks.transition.mustStartBeforeComplete',
      suggestedStatus: 'IN_PROGRESS',
      suggestedActionKey: 'tasks.transition.moveToInProgress',
    };
  }

  if (from === 'TODO' && to === 'REVIEW') {
    return {
      allowed: false,
      messageKey: 'tasks.transition.mustStartBeforeReview',
      suggestedStatus: 'IN_PROGRESS',
      suggestedActionKey: 'tasks.transition.moveToInProgress',
    };
  }

  if (from === 'BLOCKED' && (to === 'DONE' || to === 'REVIEW')) {
    return {
      allowed: false,
      messageKey: 'tasks.transition.mustUnblockFirst',
      suggestedStatus: 'IN_PROGRESS',
      suggestedActionKey: 'tasks.transition.moveToInProgress',
    };
  }

  if (from === 'DONE' && to === 'BLOCKED') {
    return {
      allowed: false,
      messageKey: 'tasks.transition.cannotBlockDone',
      suggestedStatus: 'IN_PROGRESS',
      suggestedActionKey: 'tasks.transition.reopenToInProgress',
    };
  }

  if (from === 'DONE' && to === 'REVIEW') {
    return {
      allowed: false,
      messageKey: 'tasks.transition.cannotReviewDone',
      suggestedStatus: 'IN_PROGRESS',
      suggestedActionKey: 'tasks.transition.reopenToInProgress',
    };
  }

  if (from === 'REVIEW' && to === 'TODO') {
    return {
      allowed: false,
      messageKey: 'tasks.transition.reviewCannotGoTodo',
      suggestedStatus: 'IN_PROGRESS',
      suggestedActionKey: 'tasks.transition.moveToInProgress',
    };
  }

  return {
    allowed: false,
    messageKey: 'tasks.transition.notAllowed',
  };
}
