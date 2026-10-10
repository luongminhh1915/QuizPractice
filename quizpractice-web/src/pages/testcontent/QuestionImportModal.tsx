import { useState } from 'react'
import { Alert, Button, Flex, Form, Modal, Select, Statistic, Table, Typography, Upload } from 'antd'
import { DownloadOutlined, FileExcelOutlined, UploadOutlined } from '@ant-design/icons'
import { questionsApi, type ImportResult, type QuestionLookup } from '../../api/questionsApi'
import { errorMessage, problemOf } from '../../api/client'
import { IMPORT_MAX_BYTES } from '../../constants/fieldLength'
import { useImportQuestions } from '../../hooks/useQuestions'

interface QuestionImportModalProps {
  /** The subjects the user may reach (BR-11). */
  subjects: QuestionLookup[]
  /** The subject Questions List is filtered by, filled in first. */
  defaultSubjectId?: number
  /** result is set when questions were imported, so the list can show them. */
  onClose: (result?: ImportResult) => void
}

/** The messages of a refused import: the field errors of a 400 (E2), or the detail of a 403 (E1). */
function refusal(error: unknown): string[] {
  const fields = Object.values(problemOf(error)?.errors ?? {}).flat()
  return fields.length > 0 ? fields : [errorMessage(error)]
}

/**
 * Question Import (SRS III 7.3; PDF II.7, F#32; UC-55 Import Questions): a pop-up over
 * Questions List. First the subject and the file, with the sample template of the
 * subject (A1); then the result: rows imported as Hidden and rows rejected by line (E3).
 */
export function QuestionImportModal({ subjects, defaultSubjectId, onClose }: QuestionImportModalProps) {
  const [subjectId, setSubjectId] = useState<number | undefined>(
    defaultSubjectId ?? (subjects.length === 1 ? subjects[0].id : undefined),
  )
  const [file, setFile] = useState<File>()
  const [fileProblem, setFileProblem] = useState<string>()
  const importing = useImportQuestions()
  const result = importing.data

  const choose = (chosen: File) => {
    if (!chosen.name.toLowerCase().endsWith('.xlsx')) {
      setFileProblem('The file must be an Excel workbook (.xlsx) made from the sample template.')
    } else if (chosen.size > IMPORT_MAX_BYTES) {
      setFileProblem('The file must be 5 MB or smaller.')
    } else {
      setFileProblem(undefined)
      setFile(chosen)
      importing.reset()
    }
    return false // never upload on its own; Import sends it
  }

  if (result) {
    return (
      <Modal
        open
        title="Question Import: result"
        width={760}
        onCancel={() => onClose(result.imported > 0 ? result : undefined)}
        footer={
          <Button type="primary" onClick={() => onClose(result.imported > 0 ? result : undefined)}>
            Close
          </Button>
        }
      >
        <Flex gap={48} style={{ margin: '8px 0 16px' }}>
          <Statistic title="Rows read" value={result.rowsRead} />
          <Statistic title="Imported as Hidden" value={result.imported} styles={{ content: { color: '#389e0d' } }} />
          <Statistic title="Rejected" value={result.rejected.length} styles={{ content: { color: result.rejected.length ? '#cf1322' : undefined } }} />
        </Flex>
        <Typography.Paragraph type="secondary">
          {result.imported > 0
            ? `The new questions of ${result.subject} are Hidden. Close shows them: open each one, check it, then Show it so it can be drawn (UC-54).`
            : 'Nothing was stored. Correct the rows below in the file and import it again.'}
        </Typography.Paragraph>
        {result.rejected.length > 0 && (
          <Table
            size="small"
            rowKey="line"
            pagination={result.rejected.length > 8 ? { pageSize: 8 } : false}
            dataSource={result.rejected}
            columns={[
              { title: 'Line', dataIndex: 'line', width: 70 },
              {
                title: 'Why the row was rejected',
                dataIndex: 'reasons',
                render: (reasons: string[]) => (
                  <ul style={{ margin: 0, paddingLeft: 18 }}>
                    {reasons.map((r) => (
                      <li key={r}>{r}</li>
                    ))}
                  </ul>
                ),
              },
            ]}
          />
        )}
      </Modal>
    )
  }

  return (
    <Modal
      open
      title="Question Import"
      width={560}
      onCancel={() => onClose()}
      footer={
        <>
          <Button onClick={() => onClose()}>Cancel</Button>
          <Button
            type="primary"
            disabled={!subjectId || !file}
            loading={importing.isPending}
            onClick={() => importing.mutate({ subjectId: subjectId!, file: file! })}
          >
            Import
          </Button>
        </>
      }
    >
      <Typography.Paragraph type="secondary">
        One question per row, in the columns of the sample template. Imported questions arrive Hidden until someone checks
        and shows them.
      </Typography.Paragraph>
      <Form layout="vertical">
        <Form.Item label="Subject" required extra="The bank the questions go into.">
          <Select
            aria-label="Subject"
            value={subjectId}
            placeholder="Choose a subject"
            onChange={(value: number) => {
              setSubjectId(value)
              importing.reset()
            }}
            options={subjects.map((s) => ({ value: s.id, label: s.name }))}
          />
        </Form.Item>
        <Form.Item label="Sample Template" extra="Headings, and the lesson, dimension and level names of the subject.">
          <Button
            icon={<DownloadOutlined />}
            disabled={!subjectId}
            href={subjectId ? questionsApi.templateUrl(subjectId) : undefined}
            download
          >
            Download the template
          </Button>
        </Form.Item>
        <Form.Item
          label="Import File"
          required
          validateStatus={fileProblem ? 'error' : undefined}
          help={fileProblem}
          extra="An .xlsx file of at most 5 MB and 500 questions."
        >
          <Flex gap={8} align="center" wrap>
            <Upload accept=".xlsx" showUploadList={false} beforeUpload={choose}>
              <Button icon={<UploadOutlined />}>{file ? 'Choose another file' : 'Choose file'}</Button>
            </Upload>
            {file && (
              <Typography.Text>
                <FileExcelOutlined style={{ color: '#389e0d' }} /> {file.name}
              </Typography.Text>
            )}
          </Flex>
        </Form.Item>
      </Form>
      {importing.isError && (
        <Alert
          type="error"
          showIcon
          title="The file was not imported"
          description={
            refusal(importing.error).length === 1 ? (
              refusal(importing.error)[0]
            ) : (
              <ul style={{ margin: 0, paddingLeft: 18 }}>
                {refusal(importing.error).map((m) => (
                  <li key={m}>{m}</li>
                ))}
              </ul>
            )
          }
        />
      )}
    </Modal>
  )
}
