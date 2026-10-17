-- drop view accounting.***_view;

create table if not exists Config.Multimedia
(
    multimediaId serial not null primary key,
    schoolyear varchar(15) not null,
    type serial not null,
    value varchar(1500) not null,
    foreign key (schoolyear) references secretary.schoolyear
    );


-- Transaction_Report_View view ---
CREATE OR REPLACE VIEW accounting.transaction_report_view
 AS
SELECT t.transactionid,
       t.number,
       s.studentid,
       concat_ws(' '::text, s.name, s.secondname, s.surname, s.secondsurname) AS studentname,
       t.total,
       t.date AS datetime,
       t.isvalid,
       e.label AS enrollmentlabel,
       CASE
           WHEN count(DISTINCT ta.typeid) = 1 THEN max(ta.typeid)
           ELSE 0
           END AS type
FROM accounting.transaction t
         JOIN secretary.student s ON t.studentid::text = s.studentid::text
     LEFT JOIN LATERAL ( SELECT e_1.label
           FROM academy.student asd
             JOIN academy.enrollment e_1 ON e_1.enrollmentid::text = asd.enrollmentid::text
          WHERE asd.studentid::text = s.studentid::text
          ORDER BY e_1.enrollmentid DESC
         LIMIT 1) e ON true
    LEFT JOIN accounting.transaction_tariff tt ON t.transactionid::text = tt.transactionid::text
    LEFT JOIN accounting.tariff ta ON tt.tariffid = ta.tariffid
GROUP BY t.transactionid, t.number, s.studentid, s.name, s.secondname, s.surname, s.secondsurname, t.total, t.date, e.label, t.isvalid;


--- Student_View View ---
CREATE VIEW secretary.student_to_list_view AS
SELECT s.studentid,
       concat_ws(' ', s.name, s.secondname, s.surname, s.secondsurname) as fullName,
       s.studentstate,
       t.name AS tutor,
       sch.label AS schoolyear,
       enr.label AS enrollment
FROM secretary.student s
         LEFT JOIN secretary.studenttutor t ON s.tutorid = t.tutorid
         LEFT JOIN
     (SELECT DISTINCT
      ON (aca.studentid) aca.studentid, aca.schoolyear, aca.enrollmentid
      FROM academy.student aca
      ORDER BY aca.studentid, aca.schoolyear DESC) aca ON aca.studentid = s.studentid
         LEFT JOIN secretary.schoolyear sch ON aca.schoolyear = sch.schoolyearid
         LEFT JOIN academy.enrollment enr ON aca.enrollmentid = enr.enrollmentid;


--- debtor_student_View View ---
CREATE VIEW accounting.debtor_student_view AS
SELECT s.studentid,
       CONCAT_WS(' ', s.name, s.secondname, s.surname, s.secondsurname) AS fullName,
       sch.schoolyearid,
       sch.label AS schoolyear,
       enr.enrollmentid,
       enr.label AS enrollment,
       COUNT(deb.tariffid) AS quantity,
       SUM(deb.amount - deb.debtbalance) AS total
FROM secretary.student s
         LEFT JOIN (SELECT DISTINCT
                    ON (aca.studentid) aca.studentid, aca.schoolyear, aca.enrollmentid
                    FROM academy.student aca
                    ORDER BY aca.studentid, aca.schoolyear DESC) aca ON aca.studentid = s.studentid
         LEFT JOIN secretary.schoolyear sch ON aca.schoolyear = sch.schoolyearid
         LEFT JOIN academy.enrollment enr ON aca.enrollmentid = enr.enrollmentid
         JOIN accounting.debthistory deb ON deb.studentid = s.studentid
         JOIN accounting.tariff t on t.tariffid = deb.tariffid
WHERE s.studentstate = TRUE
  AND t.duedate < CURRENT_DATE
  AND deb.ispaid = FALSE
GROUP BY (s.studentid, sch.schoolyearid, sch.label, enr.enrollmentid, enr.label);


-- transaction_invoice_view view
CREATE VIEW accounting.transaction_invoice_view as
SELECT t.transactionid, t.number, t.total, t.date, t.isvalid,
       CONCAT_WS(' ', u.name, u.surname) AS cashier,
       t.studentid,
       CONCAT_WS(' ', s.name, s.secondname, s.surname, s.secondsurname) AS student,
       STRING_AGG(tr.concept, ', ') AS concept
FROM accounting.transaction t
         JOIN secretary.student s ON t.studentid = s.studentid
         JOIN accounting.transaction_tariff tt ON t.transactionid = tt.transactionid
         JOIN accounting.tariff tr ON tt.tariffid = tr.tariffid
         JOIN accounting.cashier c ON c.cashierid = t.cashierid
         JOIN config.user u ON u.userid = c.userid
GROUP BY t.transactionid, s.studentid, u.userid;

-- student_register_view view
CREATE VIEW secretary.student_register_view AS
SELECT s.studentid,
       s.minedid,
       CONCAT_WS(' ', s.name, s.secondname, s.surname, s.secondsurname) AS fullName,
       s.studentstate AS isActive,
       s.sex,
       s.birthday,
       s.address,
       s.diseases,
       m.height,
       m.weight,
       t.name AS tutor,
       t.phone AS phone,
       fa.name AS fatherName,
       fa.idcard AS fatherIdCard,
       ma.name AS motherName,
       ma.idcard AS motherIdCard,
       sch.label AS schoolyear,
       e.schoolyear AS schoolyearId,
       d.educationallevel,
       d.label AS degree,
       d.tag AS degreePosition,
        RIGHT(e.label, 1) AS section,
        e.tag AS sectionPosition,
        a.createdAt AS enrollDate,
        a.isRepeating
        FROM secretary.student s
        JOIN secretary.studenttutor t ON t.tutorid = s.tutorid
        LEFT JOIN secretary.studentmeasurements m ON m.studentid = s.studentid
        LEFT JOIN secretary.studentparent fa ON fa.studentid = s.studentid AND fa.sex = true
        LEFT JOIN secretary.studentparent ma ON ma.studentid = s.studentid AND ma.sex = false
        LEFT JOIN (
        SELECT DISTINCT ON (a.studentid) *
        FROM academy.student a
        ORDER BY a.studentid, a.schoolyear DESC
        ) a ON a.studentid = s.studentid
        LEFT JOIN secretary.schoolyear sch ON sch.schoolyearid = a.schoolyear
        LEFT JOIN academy.enrollment e ON e.enrollmentid = a.enrollmentid
        LEFT JOIN secretary.degree d ON d.degreeid = e.degreeid;


-- grade_view view
CREATE VIEW academy.grade_view AS
SELECT row_number()    OVER (ORDER BY g.gradeid) AS id, g.studentId,
       sp.partialId,
       sp.subjectId,
       sp.teacherId,
       sp.enrollmentId,
       e.schoolyear as schoolyearId,
       p.semester,
       p.partial,
       g.grade,
       g.conductGrade,
       g.label
FROM academy.grade g
         JOIN academy.subject_partial sp ON sp.subjectpartialid = g.subjectpartialid
         JOIN academy.partial p ON p.partialid = sp.partialid
         JOIN academy.enrollment e ON e.enrollmentid = sp.enrollmentid;

-- conduct_grade_average_view view
CREATE VIEW academy.conduct_grade_average_view AS
SELECT gv.studentid, gv.partialid, gv.enrollmentid, gv.schoolyearid, AVG(gv.conductgrade) as conductgrade
FROM academy.grade_view gv
GROUP BY gv.studentid, gv.partialid, gv.enrollmentid, gv.schoolyearid, gv.partial;


-- grade_average_view view
CREATE OR REPLACE VIEW academy.grade_average_view
 AS
SELECT row_number() OVER (ORDER BY gv.studentid) AS id,
    gv.studentid,
       gv.partialid,
       gv.enrollmentid,
       gv.schoolyearid,
       gv.partial,
       (sum(gv.grade) + cd.conductgrade) / (count(gv.grade) + 1)::numeric AS grade,
    cd.conductgrade
FROM academy.grade_view gv
         LEFT JOIN academy.conduct_grade_average_view cd ON cd.studentid::text = gv.studentid::text AND cd.partialid = gv.partialid AND cd.enrollmentid::text = gv.enrollmentid::text AND cd.schoolyearid::text = gv.schoolyearid::text
GROUP BY gv.studentid, gv.partialid, gv.enrollmentid, gv.schoolyearid, gv.partial, cd.conductgrade;

-- subject_graded_view view
CREATE VIEW academy.subject_graded_view as
SELECT row_number()
       OVER (ORDER BY sp.subjectid) AS id, sp.subjectid,
       sp.teacherid,
       sp.partialid,
       COUNT(*) AS studentCount,
       SUM(CASE WHEN g.grade > 0 and g.conductgrade > 0 THEN 1 ELSE 0 END) AS gradedStudentCount
FROM academy.grade g
         join academy.subject_partial sp
              on sp.subjectpartialid = g.subjectpartialid
GROUP BY sp.subjectid, sp.teacherid, sp.partialid;

-- student_enroll_payment_view view
CREATE VIEW accounting.student_enroll_payment_view as
SELECT s.studentid, t.schoolyear as schoolyearid, aca.enrollmentid
FROM accounting.student s
         JOIN secretary.student ss ON ss.studentid = s.studentid
         JOIN accounting.debthistory d ON d.studentid = s.studentid
         JOIN accounting.tariff t ON t.tariffid = d.tariffid
         LEFT JOIN academy.student aca on aca.studentid = s.studentid
WHERE ss.studentstate = true
  AND t.typeid = 2
  AND CASE
          WHEN d.amount = 0 THEN 1
          ELSE (d.debtbalance / d.amount)
          END >= 0.4;

-- grade_average_view view
CREATE VIEW academy.grade_average_view AS
SELECT row_number() OVER (ORDER BY gv.studentid) AS id,
    gv.studentid, gv.partialid, gv.enrollmentid, gv.schoolyearid,
       gv.partial, (SUM(gv.grade) + cd.conductgrade) / (COUNT(gv.grade) + 1) as grade, cd.conductgrade
FROM academy.grade_view gv
         LEFT JOIN academy.conduct_grade_average_view cd ON
    cd.studentid = gv.studentid AND
    cd.partialid = gv.partialid AND
    cd.enrollmentid = gv.enrollmentid AND
    cd.schoolyearid = gv.schoolyearid
GROUP BY gv.studentid, gv.partialid, gv.enrollmentid, gv.schoolyearid, gv.partial, cd.conductgrade;


-- Transaction_Tariff_View view ---
CREATE VIEW accounting.transaction_tariff_view AS
SELECT t.tariffId,
       tra.studentId,
       t.schoolyear as schoolyearId,
       e.enrollmentId,
       t.educationalLevel,
       DATE_TRUNC('month', tra.date) as transactionDate,
       t.dueDate as tariffDueDate,
       SUM(tt.amount) as amount,
       t.typeid as tariffType
FROM accounting.transaction tra
         LEFT JOIN accounting.transaction_tariff tt ON tt.transactionId = tra.transactionid
         LEFT JOIN accounting.tariff t ON t.tariffId = tt.tariffId
         LEFT JOIN academy.student asd ON asd.studentId = tra.studentId and asd.schoolyear = t.schoolyear
         LEFT JOIN academy.enrollment e ON e.enrollmentId = asd.enrollmentId
where tra.isvalid = TRUE
GROUP BY t.tariffId, tra.studentId, t.schoolyear, e.enrollmentId, t.educationalLevel,
         DATE_TRUNC('month', tra.date), t.dueDate, t.typeid;
