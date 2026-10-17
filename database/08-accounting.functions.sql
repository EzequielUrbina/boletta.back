-- Update debt history by transactions --
CREATE OR REPLACE FUNCTION Accounting.update_debt_history()
    RETURNS TRIGGER AS $$
BEGIN
    UPDATE Accounting.DebtHistory dh
    SET debtbalance = dh.debtbalance + NEW.amount,
        isPaid = (dh.debtbalance + NEW.amount >= amount)
    FROM Accounting.Transaction t
    WHERE t.transactionId = NEW.transactionId
      AND t.studentId = dh.studentId
      AND dh.tariffId = NEW.tariffId;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_update_debt_history AFTER INSERT ON Accounting.Transaction_Tariff
    FOR EACH ROW EXECUTE FUNCTION Accounting.update_debt_history();

-- Update debt history by tariff overdue --
CREATE OR REPLACE FUNCTION Accounting.update_debt_history_by_tariff_overdue()
    RETURNS TRIGGER AS $$
BEGIN
    
    IF (new.late = TRUE) THEN

        WITH query_aux AS
                 (SELECT s.studentid, d.tariffid, round(new.amount*(1 - disc.amount)) as subtotal, new.late as islate
                  FROM accounting.debthistory d
                           JOIN accounting.student s on s.studentid = d.studentid
                           JOIN accounting.DiscountEducationalLevel disc ON disc.del = s.discountel
                  WHERE d.tariffid = new.tariffid and new.typeid = 1)

        UPDATE Accounting.DebtHistory dh
        SET subamount = q.subtotal,
            arrear = case when q.islate then round(q.subtotal*0.1) else 0.0 end
        FROM query_aux as q
        where dh.studentid = q.studentid and
            dh.tariffid = q.tariffid and 
            dh.ispaid = FALSE;

    END IF;
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_update_debt_history_by_tariff_overdue AFTER update ON Accounting.tariff
    FOR EACH ROW EXECUTE FUNCTION Accounting.update_debt_history_by_tariff_overdue();



-- Update ispaid in debt history --
CREATE OR REPLACE FUNCTION Accounting.update_ispaid_debt_history()
    RETURNS TRIGGER AS $$
BEGIN
    IF NEW.debtbalance >= (new.subamount + new.arrear) THEN
        NEW.ispaid := TRUE;
    ELSE
        NEW.ispaid := FALSE;
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_update_debt_history
    BEFORE UPDATE ON Accounting.DebtHistory
    FOR EACH ROW EXECUTE FUNCTION Accounting.update_ispaid_debt_history();




-- Update discount educational level by new --
CREATE OR REPLACE FUNCTION Accounting.update_new_discounteducationallevel_by_tariff()
    RETURNS TRIGGER AS $$
BEGIN
    if new.typeid = 1 then
        UPDATE accounting.discounteducationallevel 
        SET amount = 50 / (new.amount)
        where discountid = 2
          AND educationallevel = new.educationalLevel
          AND new.amount > 0.00001
          AND ABS(amount - (50 / new.amount)) > 0.0001;      
    end if;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_update_new_discounteducationallevel_by_tariff before insert ON Accounting.tariff
    FOR EACH ROW EXECUTE FUNCTION Accounting.update_new_discounteducationallevel_by_tariff();



-- Update debt history by new student discount value --
CREATE OR REPLACE FUNCTION Accounting.change_student_discount() RETURNS TRIGGER language plpgsql as
$$
BEGIN
    WITH query_aux AS
             (SELECT d.studentid, d.tariffid, round(t.amount * (1 - disc.amount)) as subtotal, t.late as islate
              FROM accounting.debthistory d
                       JOIN accounting.tariff t ON t.tariffid = d.tariffid
                       JOIN accounting.discounteducationallevel disc ON disc.del = new.discountel
              WHERE d.studentid = new.studentid
                and t.typeid = 1 and d.ispaid = FALSE)

    UPDATE accounting.debthistory
    SET subamount = q.subtotal,
        arrear    = case when q.islate then round(q.subtotal * 0.1) else 0.0 end
    FROM query_aux q
    WHERE debthistory.studentid = q.studentid
      AND debthistory.tariffid = q.tariffid;

    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_change_student_discount
    AFTER UPDATE
    ON accounting.student
    FOR EACH ROW
EXECUTE FUNCTION Accounting.change_student_discount();