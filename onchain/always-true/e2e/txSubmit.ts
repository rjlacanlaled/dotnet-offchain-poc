import { Blockfrost, Lucid } from "https://deno.land/x/lucid@0.10.7/mod.ts";

const lucid = await Lucid.new(
  new Blockfrost(
    "https://cardano-preview.blockfrost.io/api/v0",
    "previewSzjI8VuP5fLfLNTANHAXdz4iF3Jrkf0A",
  ),
  "Preview",
);

lucid.selectWalletFromSeed(
  "wink physical know way mix unable often muffin flash grape arrive supply cloud tool grain erase friend gaze family chase bachelor someone cradle inmate",
  {
    accountIndex: 0,
  },
);

const txRaw = await Deno.readTextFile("../../../tx.raw");
console.log({ txRaw });
const tx = await lucid.provider.submitTx(txRaw);

console.log(`Tx ID: ${tx}`);
