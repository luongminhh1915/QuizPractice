import { http } from './client'

/** Matches MySubjectDto (UC-71). */
export interface MySubject {
  id: number
  name: string
  thumbnail: string
  brief: string | null
}

/** Matches LessonQuizDto. available: Take Quiz can start it; openAttemptId: the unfinished attempt to resume (A1). */
export interface LessonQuiz {
  id: number
  name: string
  questionCount: number
  durationMin: number
  passRate: number
  available: boolean
  openAttemptId: number | null
}

/** Matches LearnerLessonDto; quiz is filled for a lesson of type Quiz. */
export interface LearnerLesson {
  id: number
  name: string
  type: string
  quiz: LessonQuiz | null
}

/** Matches LearnerTopicDto; id and name are null for lessons outside any topic. */
export interface LearnerTopic {
  id: number | null
  name: string | null
  lessons: LearnerLesson[]
}

/** Matches SubjectLessonsDto. */
export interface SubjectLessons {
  subjectId: number
  subjectName: string
  topics: LearnerTopic[]
}

/** /api/my/subjects — My Subjects (UC-71 Take Lesson Quiz). */
export const mySubjectsApi = {
  /** The subjects a Paid registration covers today. */
  list: () => http.get<MySubject[]>('/my/subjects').then((r) => r.data),

  /** The Active lessons under their topics. 403 Subject.NoAccess (E1). */
  lessons: (subjectId: number) => http.get<SubjectLessons>(`/my/subjects/${subjectId}/lessons`).then((r) => r.data),

  /** A new attempt at the lesson quiz. 409 LessonQuiz.Unfinished (A1) or LessonQuiz.NotAvailable (E2). */
  start: (subjectId: number, lessonId: number) =>
    http.post<{ id: number }>(`/my/subjects/${subjectId}/lessons/${lessonId}/start`).then((r) => r.data),
}
