use aiken/transaction.{OutputReference, ScriptContext, Spend, TransactionId}

type Datum {
  number: Int,
}

type Redeemer {
  msg: Int,
}

validator {
  fn unlock(datum: Datum, redeemer: Redeemer, _context: ScriptContext) -> Bool {
    datum.number == redeemer.msg
  }
}

test must_unlock() {
  let datum = Datum { number: 42 }
  let redeemer = Redeemer { msg: 42 }
  let placeholder_utxo =
    OutputReference { transaction_id: TransactionId(""), output_index: 0 }
  let context =
    ScriptContext {
      purpose: Spend(placeholder_utxo),
      transaction: transaction.placeholder(),
    }

  unlock(datum, redeemer, context)
}

test must_not_unlock() fail {
  let datum = Datum { number: 42 }
  let redeemer = Redeemer { msg: 41 }
  let placeholder_utxo =
    OutputReference { transaction_id: TransactionId(""), output_index: 0 }
  let context =
    ScriptContext {
      purpose: Spend(placeholder_utxo),
      transaction: transaction.placeholder(),
    }

  unlock(datum, redeemer, context)
}
