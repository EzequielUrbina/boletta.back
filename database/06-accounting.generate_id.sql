create schema if not exists Accounting;

-- Generate accounting.transaction id
CREATE SEQUENCE if not exists accounting.transaction_id_seq START 1000;
CREATE SEQUENCE if not exists accounting.transaction_number_seq START 1000;

CREATE OR REPLACE FUNCTION Accounting.generate_transaction_id()
    RETURNS varchar(20) AS $$
DECLARE
    year_part CHAR(2);
    seq_part CHAR(6);
BEGIN
    year_part := TO_CHAR(NOW(), 'YY');

    seq_part := LPAD(NEXTVAL('accounting.transaction_id_seq')::TEXT, 6, '0');

    RETURN year_part || seq_part || 'tst';
END;
$$ LANGUAGE plpgsql;


-- Generate accounting.cashier id
CREATE SEQUENCE if not exists accounting.cashier_id_seq START 1;

CREATE OR REPLACE FUNCTION Accounting.generate_cashier_id()
    RETURNS varchar(20) AS $$
DECLARE
    seq_part CHAR(6);
BEGIN
    seq_part := LPAD(NEXTVAL('accounting.cashier_id_seq')::TEXT, 3, '0');

    RETURN 'caj-' || seq_part;
END;
$$ LANGUAGE plpgsql;
   
-- FUNCTION: accounting.generatestudentdebts(character varying, integer, character varying)

-- DROP FUNCTION IF EXISTS accounting.generatestudentdebts(character varying, integer, character varying);

CREATE OR REPLACE FUNCTION accounting.generatestudentdebts(
	p_student_id character varying,
	p_educational_level integer,
	p_school_year_id character varying)
    RETURNS TABLE(inserted_records integer, total_amount numeric) 
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 1000

AS $BODY$
DECLARE
v_count INT;
    v_total DECIMAL(10,2);
BEGIN
    -- Validar que el estudiante existe
    IF NOT EXISTS (SELECT 1 FROM secretary.student WHERE studentid = p_student_id) THEN
        RAISE EXCEPTION 'Estudiante con ID % no encontrado', p_student_id;
END IF;
    
    -- Validar que existe el año escolar
    IF NOT EXISTS (SELECT 1 FROM secretary.schoolyear WHERE schoolyearid = p_school_year_id) THEN
        RAISE EXCEPTION 'Año escolar con ID % no encontrado', p_school_year_id;
END IF;
    
    -- Insertar las tarifas correspondientes
INSERT INTO accounting.debthistory (
    studentid,
    tariffid,
    schoolyear,
    ispaid,
    subamount,
    arrear,
    debtbalance
)
SELECT
    p_student_id,
    t.tariffid,
    p_school_year_id,
    false,
    CASE
        WHEN t.typeid = 1 THEN t.amount * (1 - COALESCE(d.amount, 0))
        ELSE t.amount
        END,
    0,
    0
FROM accounting.tariff t
         LEFT JOIN accounting.student s ON s.studentid = p_student_id
         LEFT JOIN accounting.discounteducationallevel d ON d.del = s.discountel
WHERE t.schoolyear = p_school_year_id
  AND t.educationallevel = p_educational_level;

-- Obtener número de registros insertados
GET DIAGNOSTICS v_count = ROW_COUNT;

-- Calcular el monto total insertado
SELECT COALESCE(SUM(amount), 0) INTO v_total
FROM Accounting.debthistory
WHERE studentid = p_student_id
  AND schoolyear = p_school_year_id;

-- Retornar resultados
RETURN QUERY SELECT v_count, v_total;

RAISE NOTICE 'Proceso completado: % registros insertados, monto total: %', v_count, v_total;
END;
$BODY$;

ALTER FUNCTION accounting.generatestudentdebts(character varying, integer, character varying)
    OWNER TO "user";

