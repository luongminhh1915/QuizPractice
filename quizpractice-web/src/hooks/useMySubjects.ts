import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { mySubjectsApi } from '../api/mySubjectsApi'

// Under ['account', ...]: what the customer may reach depends on who is signed in.
export const mySubjectKeys = {
  all: ['account', 'my-subjects'] as const,
}

/** UC-71: the subjects the customer may reach. */
export function useMySubjects() {
  return useQuery({ queryKey: [...mySubjectKeys.all, 'list'], queryFn: mySubjectsApi.list })
}

/** UC-71: the lessons of one subject. */
export function useSubjectLessons(subjectId: number) {
  return useQuery({
    queryKey: [...mySubjectKeys.all, 'lessons', subjectId],
    queryFn: () => mySubjectsApi.lessons(subjectId),
    retry: false,
  })
}

/** UC-71: start a lesson quiz. Whatever the answer, the lessons and the practices reload. */
export function useStartLessonQuiz(subjectId: number) {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (lessonId: number) => mySubjectsApi.start(subjectId, lessonId),
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: mySubjectKeys.all })
      void queryClient.invalidateQueries({ queryKey: ['account', 'practices'] })
    },
  })
}
